// so the puddle system relays the change through this event for ReagentFireSystem instead.
using Content.Shared.Chemistry.Components;

namespace Content.Shared._Funkystation.ReagentFires;

/// <summary>
/// Raised on a puddle entity after its puddle solution changed.
/// </summary>
[ByRefEvent]
public readonly record struct PuddleSolutionChangedEvent(Entity<SolutionComponent> Solution);
