// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Traumas/Components/BoneComponent.cs) // Inferus
// Inferus - the bone is a component on the limb organ itself, not a separate entity in a container.
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Shitmed.Surgery.Traumas;

[Serializable, NetSerializable]
public enum BoneSeverity
{
    Normal = 0,
    Damaged = 1,
    Cracked = 2,
    Broken = 3,
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BoneComponent : Component
{
    [DataField, AutoNetworkedField]
    public FixedPoint2 IntegrityCap = 60;

    [DataField, AutoNetworkedField]
    public FixedPoint2 BoneIntegrity = 60;

    [ViewVariables, AutoNetworkedField]
    public BoneSeverity BoneSeverity = BoneSeverity.Normal;

    /// <summary>
    /// The lowest integrity at which the bone is still in each severity.
    /// </summary>
    [DataField]
    public Dictionary<BoneSeverity, FixedPoint2> Thresholds = new()
    {
        { BoneSeverity.Normal, 40 },
        { BoneSeverity.Damaged, 25 },
        { BoneSeverity.Cracked, 10 },
        { BoneSeverity.Broken, 0 },
    };

    /// <summary>
    /// How much of a hit's wound damage becomes bone damage, per damage type. A type that is not listed never hurts the bone.
    /// </summary>
    [DataField]
    public Dictionary<string, float> DamageFactors = new()
    {
        { "Blunt", 1f },
        { "Slash", 0.3f },
        { "Piercing", 0.4f },
    };

    /// <summary>
    /// Hits that deal less wound damage than this never hurt the bone.
    /// </summary>
    [DataField]
    public FixedPoint2 MinimumDamage = 5;

    /// <summary>
    /// A hit this strong always rolls a success, weaker hits scale down to the minimum.
    /// </summary>
    [DataField]
    public float FullChanceDamage = 20f;

    /// <summary>
    /// Integrity regained per second while the bone is only damaged. Cracked and broken bones need treatment.
    /// </summary>
    [DataField]
    public FixedPoint2 NaturalHealing = 0.05;
}
