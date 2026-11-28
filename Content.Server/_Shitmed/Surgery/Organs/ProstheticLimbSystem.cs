// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - fills a prosthetic limb with its organs when it is created, so that it looks like the severed limb of a body.
using Content.Shared._Shitmed.Surgery.Operations;
using Content.Shared._Shitmed.Surgery.Organs;
using Content.Shared.Body;
using Robust.Shared.Containers;

namespace Content.Server._Shitmed.Surgery.Organs;

public sealed partial class ProstheticLimbSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private OrganRelationSystem _organRelation = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<ProstheticLimbComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<BodyComponent>(ent, out var body) || body.Organs is not { } container)
            return;

        EntityUid? limb = null;
        foreach (var proto in ent.Comp.Organs)
        {
            var organ = Spawn(proto, Transform(ent).Coordinates);
            if (!_container.Insert(organ, container, force: true))
            {
                Del(organ);
                continue;
            }

            if (limb == null)
                limb = organ;
            else
                _organRelation.Relate(limb.Value, organ);
        }

        if (limb == null)
            return;

        EnsureComp<DetachedLimbComponent>(ent).Limb = limb;
    }
}
