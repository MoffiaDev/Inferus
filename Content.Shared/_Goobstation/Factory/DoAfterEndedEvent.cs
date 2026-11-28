// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Goobstation.Factory;

/// <summary>
/// Event raised on the doafter user after a doafter ends.
/// </summary>
[ByRefEvent]
public readonly record struct DoAfterEndedEvent(EntityUid? Target, bool Cancelled);
