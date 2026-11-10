using System.Linq;
using Content.Server.Preferences.Managers;
using Content.Shared._Floof.Consent;
using Content.Shared._Floof.Consent.Systems;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Ghost.Components;
using Content.Shared.Preferences;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._Floof.Consent.Systems;

public sealed class ConsentSystem : SharedConsentSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IServerPreferencesManager _prefs = default!; // Inferus

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ConsentUpdateRequestEvent>(OnUpdateRequest);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawn); // Inferus
    }

    // Inferus - Remember which character slot the body came from
    private void OnPlayerSpawn(PlayerSpawnCompleteEvent ev)
    {
        if (!_prefs.TryGetCachedPreferences(ev.Player.UserId, out var prefs))
            return;

        foreach (var (slot, profile) in prefs.Characters)
        {
            if (!ReferenceEquals(profile, ev.Profile) && !profile.MemberwiseEquals(ev.Profile))
                continue;

            var comp = EnsureComp<ConsentProfileSlotComponent>(ev.Mob);
            comp.User = ev.Player.UserId;
            comp.Slot = slot;
            return;
        }
    }

    /// <summary>
    ///     Applies the in-round consent window to the player's body, and to its character slot if it is one of theirs.
    /// </summary>
    private async void OnUpdateRequest(ConsentUpdateRequestEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid || HasComp<GhostComponent>(uid))
            return;

        var text = HumanoidCharacterProfile.SanitizeConsentText(msg.Text, _cfg.GetCVar(CCVars.MaxConsentTextLength));
        var toggles = msg.Toggles.Where(t => _prototype.HasIndex(t)).ToHashSet();

        if (text != "")
        {
            var examinable = EnsureComp<ConsentExaminableComponent>(uid);
            examinable.Content = text;
            Dirty(uid, examinable);
        }
        else
            RemComp<ConsentExaminableComponent>(uid);

        if (toggles.Count > 0)
        {
            var comp = EnsureComp<ConsentTogglesComponent>(uid);
            comp.Toggles = toggles;
            Dirty(uid, comp);
        }
        else
            RemComp<ConsentTogglesComponent>(uid);

        // Inferus - Save to the character slot the body came from
        if (!TryComp<ConsentProfileSlotComponent>(uid, out var slot)
            || slot.User != args.SenderSession.UserId
            || !_prefs.TryGetCachedPreferences(slot.User, out var prefs)
            || !prefs.Characters.TryGetValue(slot.Slot, out var profile))
            return;

        await _prefs.SetProfile(slot.User, slot.Slot, profile.WithConsentText(text).WithConsentToggles(toggles), select: false);
        _prefs.SendPreferences(args.SenderSession);
    }
}
