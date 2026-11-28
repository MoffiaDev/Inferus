// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Wounds.Components;

/// <summary>
/// Marks a body whose damage is held by wounds on its limbs instead of by <c>InjurableComponent</c>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WoundDamageModelComponent : Component
{
    /// <summary>
    /// Damage that no wound holds: damage types without a wound prototype, and overflow once the limbs have no integrity left.
    /// It is what keeps a body killable after every limb is used up.
    /// </summary>
    [DataField]
    public DamageSpecifier BodyDamage = new();

    // Inferus - the values below are tunable per prototype. They add "shock" damage on top of the wound damage,
    // so a mangled head or a missing torso can crit or kill a body even when the wounds alone would not.

    /// <summary>
    /// Limbs the body cannot live without. A vital limb that is missing counts as severed.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>> VitalLimbs = new() { "Head", "Torso" };

    /// <summary>
    /// Inferus - organs the body cannot live without. Taking one out adds <see cref="VitalOrganDamage"/> of shock.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<OrganCategoryPrototype>> VitalOrgans = new() { "Heart", "Brain" };

    /// <summary>
    /// Inferus - the shock added for every vital organ that was taken out. Keep it above the body's death threshold.
    /// </summary>
    [DataField]
    public FixedPoint2 VitalOrganDamage = 300;

    /// <summary>
    /// Shock damage added for every limb at the given severity.
    /// </summary>
    [DataField]
    public Dictionary<WoundableSeverity, FixedPoint2> SeverityDamage = new()
    {
        { WoundableSeverity.Mangled, 20 },
    };

    /// <summary>
    /// Extra shock damage added for a vital limb at the given severity. Keep Mangled and Severed above the body's death threshold if they should kill.
    /// </summary>
    [DataField]
    public Dictionary<WoundableSeverity, FixedPoint2> VitalSeverityDamage = new()
    {
        { WoundableSeverity.Critical, 40 },
        { WoundableSeverity.Mangled, 300 },
        { WoundableSeverity.Severed, 300 },
    };

    /// <summary>
    /// The damage type shock damage is dealt as.
    /// </summary>
    [DataField]
    public ProtoId<DamageTypePrototype> ShockDamageType = "OrganShock"; // Inferus - was "Blunt", which also gibbed headless bodies
}
