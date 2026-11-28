// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Goob-Station (Content.Goobstation.Common/DelayedDeath/DelayedDeathEvent.cs) // Inferus
namespace Content.Shared._Shitmed.DelayedDeath;

/// <summary>
/// Raised on a user when delayed death is triggered on them, they die to it. Cancel it to save them.
/// </summary>
[ByRefEvent]
public record struct DelayedDeathEvent(EntityUid User, bool Cancelled = false, bool PreventRevive = true);