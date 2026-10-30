using System.Linq;
using Content.Shared._Floof.Consent;
using Content.Shared._Floof.Consent.Systems;
using Content.Shared.CCVar;
using Content.Shared.Ghost.Components;
using Content.Shared.Preferences;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Server._Floof.Consent.Systems;

public sealed class ConsentSystem : SharedConsentSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<ConsentUpdateRequestEvent>(OnUpdateRequest);
    }

    /// <summary>
    ///     Applies the in-round consent window to the player's body, and to its character slot if it is one of theirs.
    /// </summary>
    private void OnUpdateRequest(ConsentUpdateRequestEvent msg, EntitySessionEventArgs args)
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
    }
}
