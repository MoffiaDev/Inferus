// SharedPuddleSystem already subscribes to SolutionChangedEvent on PuddleComponent, and only one system can,
// so the puddle system relays the change through this event for ReagentFireSystem instead.
using Content.Shared.Chemistry.Components;

namespace Content.Shared._Funkystation.ReagentFires;

/// <summary>
/// Raised on a puddle entity after its puddle solution changed.
/// </summary>
[ByRefEvent]
public readonly record struct PuddleSolutionChangedEvent(Entity<SolutionComponent> Solution);
