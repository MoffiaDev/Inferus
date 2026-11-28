// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - see TemporaryHologramComponent. TimedDespawn did not remove the cuffs once they were on a person, and a person who broke free left them lying around.
using Content.Shared._Shitmed.HoloItem;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Server._Shitmed.HoloItem;

public sealed partial class TemporaryHologramSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TemporaryHologramComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<TemporaryHologramComponent, EntGotRemovedFromContainerMessage>(OnRemovedFromContainer);
    }

    private void OnMapInit(Entity<TemporaryHologramComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.ExpiresAt = _timing.CurTime + ent.Comp.Lifetime;
    }

    private void OnRemovedFromContainer(Entity<TemporaryHologramComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        // The only container the hologram is put in is the cuffs of the person, so it is gone as the cuffs come off
        QueueDel(ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TemporaryHologramComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (_timing.CurTime >= comp.ExpiresAt)
                QueueDel(uid);
        }
    }
}
