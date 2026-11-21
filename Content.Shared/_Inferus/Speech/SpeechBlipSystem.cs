using System.Text;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Mobs.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Silicons.StationAi;
using Content.Shared.Speech;
using Robust.Shared.Network;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Inferus.Speech;

/// <summary>
/// Handles assigning speech blips to entities and picking which clip a blip plays.
/// </summary>
public sealed partial class SpeechBlipSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;

    /// <summary>
    /// The blip new characters start with.
    /// </summary>
    public static readonly ProtoId<SpeechBlipPrototype> DefaultBlip = "Tenor";

    /// <summary>
    /// The blip used by silicons, machines and other robotic things that talk.
    /// </summary>
    public static readonly ProtoId<SpeechBlipPrototype> SyntheticBlip = "Synthetic";

    private readonly Dictionary<ProtoId<SpeechBlipPrototype>, List<SpeechBlipClip>> _clips = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<SpeechComponent, MapInitEvent>(OnSpeechMapInit);
    }

    /// <summary>
    /// Gives anything that talks without a blip yet (NPCs, silicons, machines) a default one,
    /// so its speech types out instead of using the plain bubble. Player profiles override this when applied.
    /// </summary>
    private void OnSpeechMapInit(Entity<SpeechComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient || HasComp<SpeechBlipComponent>(ent))
            return;

        SetBlip(ent, GetDefaultBlip(ent));
    }

    /// <summary>
    /// Picks the blip an entity should use if nothing else sets one.
    /// Humanoids use their species' blip, silicons use <see cref="SyntheticBlip"/>,
    /// then <see cref="SpeechBlipPrototype.SpeechSounds"/> is checked.
    /// Talking non-mobs (vending machines, computers, toys) are treated as machines.
    /// </summary>
    public ProtoId<SpeechBlipPrototype> GetDefaultBlip(Entity<SpeechComponent> ent)
    {
        if (TryComp<HumanoidProfileComponent>(ent, out var profile))
            return GetSpeciesBlip(_prototype, profile.Species);

        if (HasComp<SiliconLawBoundComponent>(ent)
            || HasComp<BorgChassisComponent>(ent)
            || HasComp<StationAiHeldComponent>(ent))
            return SyntheticBlip;

        if (ent.Comp.SpeechSounds is { } sounds)
        {
            foreach (var blip in _prototype.EnumeratePrototypes<SpeechBlipPrototype>())
            {
                if (blip.SpeechSounds.Contains(sounds))
                    return blip.ID;
            }
        }

        return HasComp<MobStateComponent>(ent) ? DefaultBlip : SyntheticBlip;
    }

    /// <summary>
    /// Gets the default blip for a species, from <see cref="SpeechBlipPrototype.Species"/>.
    /// Falls back to <see cref="DefaultBlip"/> if no blip lists the species.
    /// </summary>
    public static ProtoId<SpeechBlipPrototype> GetSpeciesBlip(IPrototypeManager prototype, ProtoId<SpeciesPrototype> species)
    {
        foreach (var blip in prototype.EnumeratePrototypes<SpeechBlipPrototype>())
        {
            if (blip.Species.Contains(species))
                return blip.ID;
        }

        return DefaultBlip;
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<SpeechBlipPrototype>() || args.WasModified<SoundCollectionPrototype>())
            _clips.Clear();
    }

    /// <summary>
    /// Sets the speech blip used by an entity, adding <see cref="SpeechBlipComponent"/> if needed.
    /// </summary>
    public void SetBlip(EntityUid uid, ProtoId<SpeechBlipPrototype> blip)
    {
        var comp = EnsureComp<SpeechBlipComponent>(uid);
        if (comp.Blip == blip)
            return;

        comp.Blip = blip;
        Dirty(uid, comp);
    }

    /// <summary>
    /// True if the entity's speech bubbles type out letter by letter.
    /// </summary>
    public bool UsesTypewriter(EntityUid uid)
    {
        return TryComp<SpeechBlipComponent>(uid, out var comp)
               && _prototype.TryIndex(comp.Blip, out var blip)
               && blip.Typewriter;
    }

    /// <summary>
    /// Gets every clip a blip can play, with sound collections split into one clip per file.
    /// </summary>
    public IReadOnlyList<SpeechBlipClip> GetClips(SpeechBlipPrototype blip)
    {
        if (_clips.TryGetValue(blip.ID, out var cached))
            return cached;

        var clips = new List<SpeechBlipClip>();

        if (blip.Sound != null)
            AddClips(blip.Sound, clips);

        foreach (var sound in blip.Sounds)
        {
            AddClips(sound, clips);
        }

        _clips[blip.ID] = clips;
        return clips;
    }

    private void AddClips(SoundSpecifier sound, List<SpeechBlipClip> clips)
    {
        switch (sound)
        {
            case SoundPathSpecifier path:
                clips.Add(new SpeechBlipClip(new ResolvedPathSpecifier(path.Path), path.Params));
                break;

            case SoundCollectionSpecifier { Collection: { } collection } specifier:
                if (!_prototype.TryIndex(collection, out var collectionProto))
                    break;

                for (var i = 0; i < collectionProto.PickFiles.Count; i++)
                {
                    clips.Add(new SpeechBlipClip(new ResolvedCollectionSpecifier(collection, i), specifier.Params));
                }

                break;
        }
    }

    /// <summary>
    /// Picks the clip to play for a letter.
    /// </summary>
    /// <param name="blip">The blip to pick from.</param>
    /// <param name="letter">The letter that just appeared.</param>
    /// <param name="sequence">Counter used by <see cref="SpeechBlipSelection.Sequential"/>. Keep one per message.</param>
    /// <param name="last">The last clip index played, used to avoid repeats. Start at -1.</param>
    public SpeechBlipClip? PickClip(SpeechBlipPrototype blip, Rune letter, ref int sequence, ref int last)
    {
        var clips = GetClips(blip);
        if (clips.Count == 0)
            return null;

        if (clips.Count == 1)
            return clips[0];

        int index;
        switch (blip.Selection)
        {
            case SpeechBlipSelection.Sequential:
                index = sequence % clips.Count;
                sequence++;
                break;

            case SpeechBlipSelection.Letter:
                index = (int) ((uint) Rune.ToLowerInvariant(letter).Value * 2654435761u % (uint) clips.Count);
                break;

            default:
                if (last < 0 || last >= clips.Count)
                {
                    index = _random.Next(clips.Count);
                }
                else
                {
                    index = _random.Next(clips.Count - 1);
                    if (index >= last)
                        index++;
                }

                break;
        }

        last = index;
        return clips[index];
    }
}

/// <summary>
/// One clip a speech blip can play.
/// </summary>
public readonly record struct SpeechBlipClip(ResolvedSoundSpecifier Sound, AudioParams Params);
