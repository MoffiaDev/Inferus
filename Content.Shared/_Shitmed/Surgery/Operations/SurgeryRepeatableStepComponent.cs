// SPDX-License-Identifier: AGPL-3.0-or-later
namespace Content.Shared._Shitmed.Surgery.Operations;

/// <summary>
/// The step starts again by itself after each go, until it counts as done. Moving away or being interrupted stops it.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryRepeatableStepComponent : Component;
