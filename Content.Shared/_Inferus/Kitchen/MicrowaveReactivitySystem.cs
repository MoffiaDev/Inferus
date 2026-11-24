using Content.Shared.Access.Components;
using Content.Shared.Materials;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._Inferus.Kitchen;

/// <summary>
/// Classifies generic microwave hazards identically for cooking and the chamber UI.
/// Special BeingMicrowavedEvent behavior remains with the original item systems.
/// </summary>
public sealed class MicrowaveReactivitySystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    private static readonly ProtoId<MicrowaveReactionProfilePrototype> Profile = "InferusMicrowaveReactions";

    public bool IsReactive(EntityUid uid)
    {
        // IDs have their own access, burning and unsafe-microwave outcomes. Never classify them generically.
        if (!EntityManager.EntityExists(uid) || HasComp<IdCardComponent>(uid))
            return false;

        var profile = _prototypes.Index(Profile);
        if (_whitelist.IsWhitelistPass(profile.Whitelist, uid))
            return true;

        if (!TryComp<PhysicalCompositionComponent>(uid, out var composition))
            return false;

        foreach (var (material, amount) in composition.MaterialComposition)
        {
            if (amount > 0 && IsMetal(material, profile))
                return true;
        }

        return false;
    }

    private bool IsMetal(ProtoId<MaterialPrototype> material, MicrowaveReactionProfilePrototype profile)
    {
        if (profile.Metals.Contains(material))
            return true;

        // Material inheritance is validated by the prototype manager; custom Steel-derived alloys work too.
        if (!_prototypes.TryIndex(material, out MaterialPrototype? prototype) || prototype.Parents is not { } parents)
            return false;

        foreach (var parent in parents)
        {
            if (IsMetal(parent, profile))
                return true;
        }

        return false;
    }
}
