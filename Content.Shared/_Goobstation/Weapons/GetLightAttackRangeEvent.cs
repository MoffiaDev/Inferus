// Ported from Goob Station (https://github.com/Goob-Station/Goob-Station), AGPL-3.0-or-later.
namespace Content.Shared._Goobstation.Weapons; // Inferus: was Content.Goobstation.Common.Weapons

[ByRefEvent]
public record struct GetLightAttackRangeEvent(EntityUid? Target, EntityUid User, float Range, bool Cancel = false);
