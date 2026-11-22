// Ported from Goob Station (https://github.com/Goob-Station/Goob-Station), AGPL-3.0-or-later.
namespace Content.Shared._Goobstation.Weapons.Ranged;

// todo: get event names closer to the length of the bible
[ByRefEvent]
public record struct RechargeBasicEntityAmmoGetCooldownModifiersEvent(
    float Multiplier
);
