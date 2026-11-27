// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - raised on a limb when a hit turned into wound damage on it.
using Content.Shared.FixedPoint;

namespace Content.Shared._Shitmed.Surgery.Wounds;

[ByRefEvent]
public record struct WoundableDamagedEvent(string DamageType, FixedPoint2 Amount);
