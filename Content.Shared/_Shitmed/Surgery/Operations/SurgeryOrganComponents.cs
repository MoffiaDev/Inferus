// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Steps/SurgeryOrganStepComponents) // Inferus
// Inferus - what the organ surgeries do. Effects go on step prototypes, the condition on surgery prototypes.
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Operations;

/// <summary>
/// The step takes the internal organ of this category out of the limb and into the hand of the surgeon.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryExtractOrganStepComponent : Component
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;
}

/// <summary>
/// The step puts the organ held in the hand into the limb. It has to be an organ of this category.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryImplantOrganStepComponent : Component
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;
}

/// <summary>
/// The surgery is only offered for a limb that has the internal organ of this category (<see cref="Present"/>),
/// or for a limb of the <see cref="Host"/> category that lacks it.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryOrganConditionComponent : Component
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;

    /// <summary>
    /// True: the organ has to be in the limb. False: it has to be missing.
    /// </summary>
    [DataField]
    public bool Present = true;

    /// <summary>
    /// For a missing organ, the limb it belongs in, like Torso or Head.
    /// </summary>
    [DataField]
    public ProtoId<OrganCategoryPrototype>? Host;
}
