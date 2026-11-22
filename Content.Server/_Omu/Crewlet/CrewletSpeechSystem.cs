// SPDX-License-Identifier: MIT

using System.Linq;
using System.Text.RegularExpressions;
using Content.Shared._Omu.Crewlet;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Server.Mind;
using Content.Shared.ActionBlocker;
using Content.Shared.Animals.Components;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Mobs.Systems;
using Content.Shared.Speech;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Omu.Crewlet;

public sealed partial class CrewletSpeechSystem : EntitySystem // Inferus
{
    [Dependency] private IAdminLogManager _adminLogger = default!; // Inferus
    [Dependency] private IGameTiming _timing = default!; // Inferus
    [Dependency] private IRobustRandom _random = default!; // Inferus
    [Dependency] private ActionBlockerSystem _actionBlocker = default!; // Inferus
    [Dependency] private ChatSystem _chat = default!; // Inferus
    [Dependency] private MindSystem _mind = default!; // Inferus
    [Dependency] private MobStateSystem _mobState = default!; // Inferus

    private static readonly Regex FirstPersonWord = new(
        @"\b(I['’]m|I['’]ve|I['’]ll|I['’]d|myself|mine|my|me|I)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrewletComponent, ListenEvent>(OnListen);
        SubscribeLocalEvent<CrewletAccentComponent, AccentGetEvent>(OnAccentGet);
        SubscribeLocalEvent<CrewletChatterComponent, MapInitEvent>(OnChatterMapInit);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;
        using var query = EntityQueryEnumerator<CrewletChatterComponent, CrewletDisguiseComponent>();
        while (query.MoveNext(out var uid, out var chatter, out var disguise))
        {
            if (_mind.TryGetMind(uid, out _, out _))
                continue;

            if (now >= chatter.NextLine)
            {
                chatter.NextLine = now + _random.Next(chatter.MinInterval, chatter.MaxInterval);
                if (TryComp<ParrotMemoryComponent>(disguise.Husk, out var memory)
                    && memory.SpeechMemories.Count > 0
                    && _actionBlocker.CanSpeak(uid))
                {
                    var line = _random.Pick(memory.SpeechMemories).Message;
                    _chat.TrySendInGameICMessage(uid, line, InGameICChatType.Speak, false);
                }
            }

            if (now < chatter.NextEmote)
                continue;

            chatter.NextEmote = now + _random.Next(chatter.MinInterval, chatter.MaxInterval);
            if (TryComp<CrewletBrainComponent>(disguise.Husk, out var brain) && brain.Emotes.Count > 0)
                _chat.TryEmoteWithChat(uid, _random.Pick(brain.Emotes), forceEmote: true);
        }
    }

    private void OnChatterMapInit(Entity<CrewletChatterComponent> ent, ref MapInitEvent args)
    {
        var now = _timing.CurTime;
        ent.Comp.NextLine = now + _random.Next(ent.Comp.MinInterval, ent.Comp.MaxInterval);
        ent.Comp.NextEmote = now + _random.Next(ent.Comp.MinInterval, ent.Comp.MaxInterval);
    }

    private void OnListen(Entity<CrewletComponent> ent, ref ListenEvent args)
    {
        // if (args.Source != ent.Comp.CrewletOwner // Inferus - learn from anything that talks, not just the owner
        if (args.Source == ent.Owner // Inferus
            || (TryComp<CrewletDisguiseComponent>(args.Source, out var disguise) && disguise.Husk == ent.Owner) // Inferus - don't relearn our own disguise's lines
            || !TryComp<ParrotMemoryComponent>(ent, out var memory)
            || _mobState.IsIncapacitated(ent)
            || _timing.CurTime < memory.NextLearnInterval)
            return;

        var message = args.Message.Trim();
        if (message.Length == 0
            || message.Contains('~')
            || message.Length < memory.MinEntryLength
            || message.Length > memory.MaxEntryLength)
            return;

        memory.NextLearnInterval = _timing.CurTime + memory.LearnCooldown;
        if (!_random.Prob(memory.LearnChance))
            return;

        _adminLogger.Add(LogType.Chat,
            LogImpact.Low,
            $"{ToPrettyString(ent.Owner):entity} learned \"{message}\" from {ToPrettyString(args.Source):speaker}");

        var userId = _mind.TryGetMind(args.Source, out _, out var mind) ? mind.UserId : null;
        var entry = new SpeechMemory(userId, message);
        if (memory.SpeechMemories.Count < memory.MaxSpeechMemory)
            memory.SpeechMemories.Add(entry);
        else
            memory.SpeechMemories[_random.Next(memory.SpeechMemories.Count)] = entry;
    }

    private void OnAccentGet(Entity<CrewletAccentComponent> ent, ref AccentGetEvent args)
    {
        var name = ent.Comp.Name;
        args.Message = FirstPersonWord.Replace(args.Message, match => ReplaceWord(match.Value, name));
    }

    private static string ReplaceWord(string word, string name)
    {
        var replacement = word.ToLowerInvariant().Replace('’', '\'') switch
        {
            "i'm" => $"{name} is",
            "i've" => $"{name} has",
            "i'll" => $"{name} will",
            "i'd" => $"{name} would",
            "my" or "mine" => $"{name}'s",
            _ => name,
        };

        var letters = word.Where(char.IsLetter).ToList();
        return letters.Count >= 2 && letters.All(char.IsUpper)
            ? replacement.ToUpperInvariant()
            : replacement;
    }
}
