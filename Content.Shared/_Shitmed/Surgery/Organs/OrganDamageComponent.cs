// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Traumas/Components/OrganDamageComponent.cs) // Inferus
// Inferus - the condition of an internal organ, modelled on the bone of a limb. It is added to every internal organ when it starts.
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Shitmed.Surgery.Organs;

[Serializable, NetSerializable]
public enum OrganSeverity
{
    Healthy = 0,
    Bruised = 1,
    Damaged = 2,
    Failed = 3,
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class OrganDamageComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 IntegrityCap = 40;

    [DataField, AutoNetworkedField]
    public FixedPoint2 Integrity = 40;

    [ViewVariables, AutoNetworkedField]
    public OrganSeverity Severity = OrganSeverity.Healthy;

    /// <summary>
    /// The lowest integrity at which the organ is still in each severity. A failed organ counts as lost for the body.
    /// </summary>
    [DataField]
    public Dictionary<OrganSeverity, FixedPoint2> Thresholds = new()
    {
        { OrganSeverity.Healthy, 28 },
        { OrganSeverity.Bruised, 14 },
        { OrganSeverity.Damaged, 1 },
        { OrganSeverity.Failed, 0 },
    };

    /// <summary>
    /// How much of a hit's wound damage on the limb the organ sits in becomes organ damage, per damage type.
    /// A type that is not listed never hurts the organ.
    /// </summary>
    [DataField]
    public Dictionary<string, float> DamageFactors = new()
    {
        { "Blunt", 0.5f },
        { "Slash", 0.3f },
        { "Piercing", 0.6f },
    };

    /// <summary>
    /// Integrity regained per second while the organ is only bruised or healthy. Damaged and failed organs need surgery.
    /// </summary>
    [DataField]
    public FixedPoint2 NaturalHealing = 0.05;
}
