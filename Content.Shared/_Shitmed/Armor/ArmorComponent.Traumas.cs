// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/Armor/ArmorComponent.cs) // Inferus
// Inferus - kept in a partial so the upstream file stays untouched. Coverage names organ categories instead of BodyPartType.
using Content.Shared._Shitmed.Surgery.Traumas;
using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Robust.Shared.Analyzers;
using Robust.Shared.Prototypes;

namespace Content.Shared.Armor;

public sealed partial class ArmorComponent
{
    /// <summary>
    /// The limbs this armour protects from traumas. Empty means all of them.
    /// </summary>
    [DataField("coverage"), Access(Other = AccessPermissions.ReadExecute)]
    public List<ProtoId<OrganCategoryPrototype>> Coverage = new();

    /// <summary>
    /// How much the armour lowers the chance of each trauma type on the limbs it covers.
    /// </summary>
    [DataField, Access(Other = AccessPermissions.ReadExecute)]
    public Dictionary<ProtoId<TraumaTypePrototype>, FixedPoint2> TraumaDeductions = new();
}