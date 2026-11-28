// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Traumas/TraumaCause.cs) // Inferus
// Inferus - allowed parts are organ categories (ArmLeft, Torso, ...) instead of BodyPartType.
using Content.Shared.Body;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Traumas;

[ImplicitDataDefinitionForInheritors]
public abstract partial class TraumaCause
{
    /// <summary>
    /// If set, this cause can only inflict the trauma on limbs of these categories. Null means any limb.
    /// </summary>
    [DataField]
    public List<ProtoId<OrganCategoryPrototype>>? AllowedParts;

    public bool PartAllowed(ProtoId<OrganCategoryPrototype>? category)
    {
        return AllowedParts == null || (category is { } cat && AllowedParts.Contains(cat));
    }
}

/// <summary>
/// Inflicts a trauma when the entity takes explosion damage.
/// </summary>
public sealed partial class ExplosionCause : TraumaCause
{
    /// <summary>
    /// Minimum total explosion damage dealt to this entity before a roll happens.
    /// </summary>
    [DataField]
    public FixedPoint2 MinDamage = 40;

    /// <summary>
    /// Base probability of inflicting the trauma once past <see cref="MinDamage"/>.
    /// </summary>
    [DataField]
    public float Chance = 0.25f;

    /// <summary>
    /// If true, the chance scales up as damage exceeds <see cref="MinDamage"/>.
    /// </summary>
    [DataField]
    public bool ScaleWithDamage = true;
}

/// <summary>
/// Inflicts a trauma when a wound of a matching damage type gains severity.
/// </summary>
public sealed partial class WoundSeverityCause : TraumaCause
{
    /// <summary>
    /// Damage types of wound that can inflict this trauma. Null or empty means any damaging wound.
    /// </summary>
    [DataField]
    public List<ProtoId<DamageTypePrototype>>? DamageTypes;

    /// <summary>
    /// Formula that computes the base chance for this cause. Null uses the BaseChance of the trauma type.
    /// </summary>
    [DataField]
    public TraumaChance? Chance;
}