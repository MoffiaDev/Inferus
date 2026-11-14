namespace Content.Shared.Weapons.Melee.Events;

/// <summary>
/// Raised directed on a weapon when attempt a melee attack.
/// </summary>
[ByRefEvent]
// Goobstation - added IsHeavyAttack for grab intent (grabbing items block
// attacks while actively grabbing, except heavy attacks at choke stage)
public record struct AttemptMeleeEvent(EntityUid User, bool IsHeavyAttack = false, bool Cancelled = false, string? Message = null);
