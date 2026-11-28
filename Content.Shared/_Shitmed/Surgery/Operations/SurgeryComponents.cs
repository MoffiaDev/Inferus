// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/SurgeryComponent.cs, Steps/SurgeryStepComponent.cs, Conditions) // Inferus
// Inferus - a surgery is an entity prototype with a list of step entity prototypes, as in Goob-Station. The entities are
// spawned once into nullspace by the server's SurgerySystem and only read from there.
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Operations;

/// <summary>
/// Put on a surgery prototype.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryComponent : Component
{
    /// <summary>
    /// The steps, in the order they have to be done.
    /// </summary>
    [DataField]
    public List<EntProtoId> Steps = new();

    /// <summary>
    /// A surgery that has to be done before this one can start.
    /// </summary>
    [DataField]
    public EntProtoId? Requirement;

    /// <summary>
    /// Lower comes first in the list.
    /// </summary>
    [DataField]
    public int Priority;
}

/// <summary>
/// Put on a surgery step prototype.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryStepComponent : Component
{
    /// <summary>
    /// The tool component the held item needs. Every component in here has to be an <see cref="ISurgeryToolComponent"/>.
    /// </summary>
    [DataField]
    public ComponentRegistry? Tool;

    /// <summary>
    /// Added to the limb when the step is done. The step counts as done when the limb has all of them.
    /// </summary>
    [DataField]
    public ComponentRegistry? Add;

    /// <summary>
    /// Removed from the limb when the step is done. The step counts as done when the limb has none of them.
    /// </summary>
    [DataField]
    public ComponentRegistry? Remove;

    [DataField]
    public ComponentRegistry? BodyAdd;

    [DataField]
    public ComponentRegistry? BodyRemove;

    /// <summary>
    /// Seconds, before the speed of the tool is applied.
    /// </summary>
    [DataField]
    public float Duration = 2f;

    /// <summary>
    /// The pain it causes if the patient is awake.
    /// </summary>
    [DataField]
    public float Pain = 15f;
}

/// <summary>
/// The surgery only shows up for a limb that has all of these components, or none of them when <see cref="Inverse"/> is set.
/// Works on both surgeries and steps.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryPartComponentConditionComponent : Component
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();

    [DataField]
    public bool Inverse;
}

/// <summary>
/// Marks an item as something that can start a surgery, through its "perform surgery" verb.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SurgeryToolComponent : Component;

#region Limb state

/// <summary>
/// The skin of the limb is cut open.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IncisionOpenComponent : Component;

/// <summary>
/// The bleeding vessels of the open incision are clamped.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BleedersClampedComponent : Component;

/// <summary>
/// The skin around the incision is held back.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SkinRetractedComponent : Component;

#endregion
