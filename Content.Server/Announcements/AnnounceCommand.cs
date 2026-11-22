using System.Linq;
using Content.Server._MACRO.Announcements;
using Content.Server.Administration;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Systems;
using Content.Shared._MACRO.Announcements;
using Content.Shared.Administration;
using Content.Shared.Database;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;

namespace Content.Server.Announcements;

[AdminCommand(AdminFlags.Moderator)]
[Access(typeof(PAAnnounceCommand))] // funky - restrict access to our PA announce "subcommand"
public sealed partial class AnnounceCommand : LocalizedEntityCommands
{
    private static readonly ProtoId<AnnouncementSoundPrototype> AnnounceId = "Announce";
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IResourceManager _res = default!;
    [Dependency] private AnnouncerManager _announcer = default!; // macrocosm
    [Dependency] private IAdminLogManager _adminLogger = default!;

    public override string Command => "announce";
    public override string Description => Loc.GetString("cmd-announce-desc");
    public override string Help => Loc.GetString("cmd-announce-help", ("command", Command));

    // funky - move command logic into a separate method so we can implement a pseudo-subcommand
    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        OnExecute(shell, argStr, args, true,
            Loc, _announcer, _chat, _adminLogger); // Inferus - pass admin logger for upstream announce logging
    }

    public static void OnExecute(IConsoleShell shell, string argStr, string[] args, bool paBypass,
        ILocalizationManager loc, AnnouncerManager announcer, ChatSystem chat, IAdminLogManager adminLogger) // Inferus - adminLogger
    {
        switch (args.Length)
        {
            case 0:
                shell.WriteError(loc.GetString("shell-need-minimum-one-argument"));
                return;
            case > 4:
                shell.WriteError(loc.GetString("shell-wrong-arguments-number"));
                return;
        }

        var message = args[0];
        var sender = loc.GetString("cmd-announce-sender");
        var color = Color.Gold;
        // Macrocosm edit - handle sound
        if (!announcer.TryGetAnnouncerSound(AnnounceId, out var sound) && args.Length < 4)
        {
            var warningMessage = loc.GetString("cmd-announce-no-sound", ("sound", AnnounceId));
            shell.WriteError(warningMessage);
        }
        // Macrocosm edit end

        // Optional sender argument
        if (args.Length >= 2)
            sender = args[1];

        // Optional color argument
        if (args.Length >= 3)
        {
            try
            {
                color = Color.FromHex(args[2]);
            }
            catch
            {
                shell.WriteError(loc.GetString("shell-invalid-color-hex"));
                return;
            }
        }

        // Optional sound argument
        if (args.Length >= 4)
        {
            var soundOverride = args[3];
            if (!announcer.TryGetAnnouncerSound(soundOverride, out sound)) // Macrocosm edit - allow announcement sound prototypes
                sound = new SoundPathSpecifier(soundOverride);
        }

        chat.DispatchGlobalAnnouncement(message, sender, true, sound, color, paSystemBypass: paBypass); // funky - add pa system bypass option

        if (shell.Player is null)
        {
            adminLogger.Add(LogType.Chat, LogImpact.Low,
                $"LOCAL used 'announce' with displayed sender {sender}: {message}");
        }
        else
        {
            adminLogger.Add(LogType.Chat, LogImpact.Low,
                $"{shell.Player:Player} used 'announce' with displayed sender {sender}: {message}");
        }

        shell.WriteLine(loc.GetString("shell-command-success"));
    }

    // funky - as above, move GetCompletion logic into a separate method so we can implement a pseudo-subcommand
    public override CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        return OnGetCompletion(shell, args,
            Loc, _proto, _res);
    }
    public static CompletionResult OnGetCompletion(IConsoleShell shell, string[] args,
        ILocalizationManager loc, IPrototypeManager proto, IResourceManager res)
    {
        return args.Length switch
        {
            1 => CompletionResult.FromHint(loc.GetString("cmd-announce-arg-message")),
            2 => CompletionResult.FromHint(loc.GetString("cmd-announce-arg-sender")),
            3 => CompletionResult.FromHint(loc.GetString("cmd-announce-arg-color")),
            4 => CompletionResult.FromHintOptions(
                CompletionHelper.AudioFilePath(args[3], proto, res)
                    .Concat(CompletionHelper.PrototypeIDs<AnnouncementSoundPrototype>(proto: proto)), // Macrocosm edit - announcer sound prototypes
                loc.GetString("cmd-announce-arg-sound")
            ),
            _ => CompletionResult.Empty
        };
    }
}
