// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - lets a step turn down the held item before the do-after starts.
namespace Content.Shared._Shitmed.Surgery.Operations;

/// <summary>
/// Raised on the step entity when a surgeon picks it, after the tool check and before the do-after starts.
/// Set <see cref="Cancelled"/> (and show a popup) to stop the step from starting.
/// </summary>
[ByRefEvent]
public record struct SurgeryStepPreCheckEvent(EntityUid User, EntityUid Body, EntityUid Part, EntityUid Tool, bool Cancelled = false);
