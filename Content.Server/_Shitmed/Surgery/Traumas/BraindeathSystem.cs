// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Server/_Shitmed/Medical/Trauma/BraindeathSystem.cs) // Inferus
using Content.Shared._Shitmed.Surgery.Traumas;
using Content.Shared.Body;

namespace Content.Server._Shitmed.Surgery.Traumas;

public sealed class BraindeathSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BraindeathComponent, TraumaInducedEvent>(OnInduced);
        SubscribeLocalEvent<BraindeathComponent, TraumaBeingRemovedEvent>(OnRemoved);
    }

    private void OnInduced(Entity<BraindeathComponent> ent, ref TraumaInducedEvent args)
    {
        if (TryGetBody(args.Trauma.Comp.HoldingWoundable, out var body))
            EnsureComp<DebrainedComponent>(body);
    }

    private void OnRemoved(Entity<BraindeathComponent> ent, ref TraumaBeingRemovedEvent args)
    {
        if (TryGetBody(args.Trauma.Comp.HoldingWoundable, out var body))
            RemComp<DebrainedComponent>(body);
    }

    private bool TryGetBody(EntityUid? woundable, out EntityUid body)
    {
        body = default;
        if (woundable == null
            || !TryComp<OrganComponent>(woundable, out var organ)
            || organ.Body is not { } found)
            return false;

        body = found;
        return true;
    }
}