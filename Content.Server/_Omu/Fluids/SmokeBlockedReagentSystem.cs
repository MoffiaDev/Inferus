// SPDX-License-Identifier: MIT

using System.Linq;
using Content.Shared.FixedPoint; // Inferus: was Content.Goobstation.Maths.FixedPoint
using Content.Shared._Omu.Fluids; // Inferus: was Content.Omu.Shared.Fluids
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Fluids;
using Robust.Shared.Prototypes;

namespace Content.Server._Omu.Fluids; // Inferus: was Content.Omu.Server.Fluids

public sealed class SmokeBlockedReagentSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutions = default!;

    private ProtoId<ReagentPrototype>[] _blocked = Array.Empty<ProtoId<ReagentPrototype>>();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SmokeComponent, SolutionChangedEvent>(OnSolutionChanged); // Inferus: was SolutionContainerChangedEvent
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        CacheBlocked();
    }

    // Inferus - SolutionContainerChangedEvent does not exist here, SolutionChangedEvent carries the solution entity instead
    private void OnSolutionChanged(Entity<SmokeComponent> ent, ref SolutionChangedEvent args)
    {
        if (args.Solution.Comp.Id != SmokeComponent.SolutionName || _blocked.Length == 0)
            return;

        var solution = args.Solution.Comp.Solution;
        var blocked = solution.GetTotalPrototypeQuantity(_blocked);
        if (blocked <= FixedPoint2.Zero)
            return;

        var removed = solution.SplitSolutionWithOnly(blocked, _blocked);
        _solutions.UpdateChemicals(args.Solution);
        _puddle.TrySpillAt(Transform(ent).Coordinates, removed, out _, false);
    }
    // End Inferus

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<SmokeBlockedReagentsPrototype>())
            CacheBlocked();
    }

    private void CacheBlocked()
    {
        _blocked = _proto.EnumeratePrototypes<SmokeBlockedReagentsPrototype>()
            .SelectMany(prototype => prototype.Reagents)
            .Distinct()
            .ToArray();
    }
}
