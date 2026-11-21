using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Inferus.Damage;

/// <summary>
/// Applies every damage modifier set in <see cref="Modifiers"/> on top of the entity's own resistances.
/// Stands in for Goob's DamageProtectionBuffComponent, which ported Omu crewlets rely on.
/// Not networked; the modifier set prototypes are not net-serializable and damage is resolved on the server.
/// </summary>
[RegisterComponent]
public sealed partial class DamageProtectionBuffComponent : Component
{
    [DataField]
    public Dictionary<ProtoId<DamageModifierSetPrototype>, DamageModifierSetPrototype> Modifiers = new();
}
