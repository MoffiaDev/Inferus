// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Traumas/Components/*) // Inferus
// Inferus - a trauma that aims at a limb of the body names the limb by organ category instead of (BodyPartType, BodyPartSymmetry).
using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Traumas;

/// <summary>
/// A trauma, an entity that lives in the trauma container of the wound that caused it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TraumaComponent : Component
{
    /// <summary>
    /// The limb the wound that caused this trauma is on. Null if that limb is gone.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? HoldingWoundable;

    /// <summary>
    /// For OrganDamage the organ. For BoneDamage the limb that carries the bone. For VeinsDamage and NerveDamage the limb.
    /// For Dismemberment the parent of the limb that was torn off.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? TraumaTarget;

    /// <summary>
    /// For Dismemberment, the category of the limb that was torn off.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public ProtoId<OrganCategoryPrototype>? TargetCategory;

    /// <summary>
    /// The severity the wound had when the trauma got induced. Updated if the same wound worsens it.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public FixedPoint2 TraumaSeverity;

    [DataField, ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public ProtoId<TraumaTypePrototype> TraumaType;
}

/// <summary>
/// Put on a wound. Rolls the traumas the damage type of the wound can cause when the wound gains severity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TraumaInflicterComponent : Component
{
    /// <summary>
    /// A rise in severity lower than this never induces a trauma.
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 SeverityThreshold = 9f;

    /// <summary>
    /// The container where all the traumas of this wound are stored.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Container TraumaContainer = default!;

    /// <summary>
    /// Trauma types armour is allowed to reduce the chance of. Kept so wound prototypes stay compatible with Goob.
    /// Armour does not reduce trauma chances in Inferus yet.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public List<ProtoId<TraumaTypePrototype>> AllowArmourDeduction = new();

    /// <summary>
    /// Optional override of the entity spawned for a trauma type, instead of the one of the trauma type.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public Dictionary<ProtoId<TraumaTypePrototype>, EntProtoId> TraumaPrototypes = new();

    /// <summary>
    /// Added to the chance of a trauma type. Negative values make it rarer.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<TraumaTypePrototype>, FixedPoint2> TraumasChances = new();

    /// <summary>
    /// When a wound is mangled, damage it receives is multiplied by these values and applied to the respective body elements.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<TraumaTypePrototype>, FixedPoint2>? MangledMultipliers;
}

/// <summary>
/// Keeps a constant bleed on the body while the trauma is on it.
/// </summary>
[RegisterComponent]
public sealed partial class TraumaBloodlossComponent : Component
{
    /// <summary>
    /// Constant bleed floor this trauma maintains on the body.
    /// </summary>
    [DataField]
    public float Amount = 0.6f;
}

/// <summary>
/// The body has lost its brain while the trauma is on it.
/// </summary>
[RegisterComponent]
public sealed partial class BraindeathComponent : Component;

/// <summary>
/// An organ that causes this trauma on its limb when it is destroyed.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class OrganDeathTraumaComponent : Component
{
    [DataField(required: true)]
    public ProtoId<TraumaTypePrototype> Trauma;
}

/// <summary>
/// A limb without a real bone, it is harder to tear off.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BonelessComponent : Component
{
    /// <summary>
    /// The higher it is, the lower the chance to delimb. A perfectly healed bone is 1, a destroyed bone is 0.
    /// See DismembermentChance in TraumaChance.cs.
    /// </summary>
    [ViewVariables, DataField]
    public FixedPoint2 BonePenalty = 1;
}