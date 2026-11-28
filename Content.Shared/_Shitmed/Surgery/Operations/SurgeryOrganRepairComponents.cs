// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - the organ repair surgery.
using Content.Shared.FixedPoint;

namespace Content.Shared._Shitmed.Surgery.Operations;

/// <summary>
/// The step heals every hurt internal organ in the limb. It repeats until none are left.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryTendOrgansStepComponent : Component
{
    [DataField]
    public FixedPoint2 Amount = 10;
}

/// <summary>
/// The surgery is only offered for a limb that has hurt internal organs.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryOrgansDamagedConditionComponent : Component;
