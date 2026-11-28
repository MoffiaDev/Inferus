// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Steps, Conditions, Effects) // Inferus
// Inferus - what the limb, bone and wound surgeries do. Effects go on step prototypes, conditions on surgery prototypes.
using Content.Shared._Shitmed.Surgery.Traumas;
using Content.Shared.FixedPoint;

namespace Content.Shared._Shitmed.Surgery.Operations;

#region Step effects

/// <summary>
/// The step cuts the limb it is done on off the body.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryAmputateStepComponent : Component;

/// <summary>
/// The step attaches the severed limb in the hand to the part it is done on.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryAttachLimbStepComponent : Component;

/// <summary>
/// The step finishes a reattachment: the bone is restored and the markers are taken off.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryFinishReattachStepComponent : Component;

/// <summary>
/// The step changes the integrity of the bone of the limb.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryBoneStepComponent : Component
{
    /// <summary>
    /// Sets the integrity to this.
    /// </summary>
    [DataField]
    public FixedPoint2? Set;

    /// <summary>
    /// Sets the integrity to the full amount, ignoring <see cref="Set"/>.
    /// </summary>
    [DataField]
    public bool Restore;
}

/// <summary>
/// The step heals wounds of these damage types on the limb. It repeats until the limb has none left.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryTendWoundsStepComponent : Component
{
    [DataField(required: true)]
    public List<string> Types = new();

    [DataField]
    public FixedPoint2 Amount = 20;
}

#endregion

#region Conditions

/// <summary>
/// The surgery is only offered for a limb that can be cut off the body.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryAmputableConditionComponent : Component;

/// <summary>
/// The surgery is only offered for a limb with a bone at least this damaged, or one that is half way through being mended.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryBoneConditionComponent : Component
{
    [DataField]
    public BoneSeverity Minimum = BoneSeverity.Cracked;
}

/// <summary>
/// The surgery is only offered for a limb with wounds of these damage types.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryWoundedConditionComponent : Component
{
    [DataField(required: true)]
    public List<string> Types = new();
}

/// <summary>
/// The surgery is only offered for a part that a severed limb nearby fits on, or that has a limb that is half way through being attached.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryReattachConditionComponent : Component;

#endregion
