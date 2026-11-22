using Content.Server._MACRO.Announcements;
using Content.Server._Funkystation.Communications;
using Content.Shared._Funkystation.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.Station.Components;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    [Dependency] private AnnouncerManager _announcer = default!; // macrocosm

    [Dependency] private PASystem _paSystem = null!; // funky - announcements via PA speakers

    /// <inheritdoc />
    public override void DispatchGlobalAnnouncement(
        string message,
        string? sender = null,
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null,
        string? signature = null,
        bool paSystemBypass = false // funky
        )
    {
        sender ??= Loc.GetString("chat-manager-sender-announcement");

        // funky - redirect announcement to PA speakers
        if (!paSystemBypass && _configurationManager.GetCVar(PAAnnouncementCVars.PAEnabled))
        {
            var paExclusive = _configurationManager.GetCVar(PAAnnouncementCVars.PAExclusiveAnnouncements);
            _paSystem.DispatchPAAnnouncement(
                WithSignature(message, signature), // Inferus - keep upstream announcement signatures on PA announcements
                sender,
                source: null,
                preamble: false,
                playSound: playSound && paExclusive, // we don't want to double up on announcement sounds being played globally and through the PA speakers
                global: true,
                customPreamble: null,
                announcementSound,
                colorOverride);
            if (paExclusive)
                return;
        }

        var wrappedMessage = WrapAnnouncement(sender, message, signature);
        _chatManager.ChatMessageToAll(ChatChannel.Radio, message, wrappedMessage, default, false, true, colorOverride);
        if (playSound)
        {
            // Macrocosm edit start - announcer variation
            if (announcementSound == null)
            {
                _announcer.TryGetAnnouncerSound(DefaultAnnouncementSound, out announcementSound);
            }
            _audio.PlayGlobal(announcementSound, Filter.Broadcast(), true, AudioParams.Default.WithVolume(-2f));
            // Macrocosm edit end
        }
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Global station announcement from {sender}: {message}");
    }

    /// <inheritdoc />
    public override void DispatchFilteredAnnouncement(
        Filter filter,
        string message,
        EntityUid? source = null,
        string? sender = null,
        bool playSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null,
        string? signature = null,
        bool paSystemBypass = false) // funky
    {
        sender ??= Loc.GetString("chat-manager-sender-announcement");

        // funky - redirect announcement to PA speakers
        if (!paSystemBypass && _configurationManager.GetCVar(PAAnnouncementCVars.PAEnabled))
        {
            var paExclusive = _configurationManager.GetCVar(PAAnnouncementCVars.PAExclusiveAnnouncements);
            _paSystem.DispatchPAAnnouncement(
                WithSignature(message, signature), // Inferus - keep upstream announcement signatures on PA announcements
                sender,
                source,
                preamble: false,
                playSound: playSound && paExclusive, // we don't want to double up on announcement sounds being played globally and through the PA speakers
                global: true,
                customPreamble: null,
                announcementSound,
                colorOverride);
            if (paExclusive)
                return;
        }

        var wrappedMessage = WrapAnnouncement(sender, message, signature);
        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, wrappedMessage, source ?? default, false, true, colorOverride);
        if (playSound)
        {
            // Macrocosm edit start - announcer variation
            if (announcementSound == null)
            {
                _announcer.TryGetAnnouncerSound(DefaultAnnouncementSound, out announcementSound);
            }
            _audio.PlayGlobal(announcementSound, filter, true, AudioParams.Default.WithVolume(-2f));
            // Macrocosm edit end
        }
        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Station Announcement from {sender}: {message}");
    }

    /// <inheritdoc />
    public override void DispatchStationAnnouncement(
        EntityUid source,
        string message,
        string? sender = null,
        bool playDefaultSound = true,
        SoundSpecifier? announcementSound = null,
        Color? colorOverride = null,
        string? signature = null,
        bool paSystemBypass = false) // funky
    {
        sender ??= Loc.GetString("chat-manager-sender-announcement");

        // funky - redirect announcement to PA speakers
        if (!paSystemBypass && _configurationManager.GetCVar(PAAnnouncementCVars.PAEnabled))
        {
            var paExclusive = _configurationManager.GetCVar(PAAnnouncementCVars.PAExclusiveAnnouncements);
            _paSystem.DispatchPAAnnouncement(
                WithSignature(message, signature), // Inferus - keep upstream announcement signatures on PA announcements
                sender,
                source,
                preamble: false,
                playSound: playDefaultSound && paExclusive, // we don't want to double up on announcement sounds being played globally and through the PA speakers
                global: false,
                customPreamble: null,
                announcementSound,
                colorOverride);
            if (paExclusive)
                return;
        }

        var wrappedMessage = WrapAnnouncement(sender, message, signature);
        var station = _stationSystem.GetOwningStation(source);

        if (station == null)
        {
            // you can't make a station announcement without a station
            return;
        }

        if (!TryComp<StationDataComponent>(station, out var stationDataComp)) return;

        var filter = _stationSystem.GetInStation(stationDataComp);

        _chatManager.ChatMessageToManyFiltered(filter, ChatChannel.Radio, message, wrappedMessage, source, false, true, colorOverride);

        if (playDefaultSound)
        {
            // Macrocosm edit start - announcer variation
            if (announcementSound == null)
            {
                _announcer.TryGetAnnouncerSound(DefaultAnnouncementSound, out announcementSound);
            }
            _audio.PlayGlobal(announcementSound, filter, true, AudioParams.Default.WithVolume(-2f));
            // Macrocosm edit end
        }

        _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Station Announcement on {station} from {sender}: {message}");
    }

    // Inferus - plain-text signature for PA speaker announcements
    private static string WithSignature(string message, string? signature)
    {
        return string.IsNullOrWhiteSpace(signature) ? message : $"{message}\n{signature}";
    }

    private string WrapAnnouncement(string sender, string message, string? signature)
    {
        var escapedMessage = FormattedMessage.EscapeText(message);
        return string.IsNullOrWhiteSpace(signature)
            ? Loc.GetString("chat-manager-sender-announcement-wrap-message", ("sender", sender), ("message", escapedMessage))
            : Loc.GetString("chat-manager-sender-announcement-wrap-message-signed", ("sender", sender), ("message", escapedMessage), ("signature", FormattedMessage.EscapeText(signature)));
    }
}
