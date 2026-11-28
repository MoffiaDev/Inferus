// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - a body without a brain can not move. Shared so the client predicts it, the server system makes it fall down and keeps it from speaking.
using Content.Shared.ActionBlocker;
using Content.Shared.Movement.Events;

namespace Content.Shared._Shitmed.Surgery.Traumas.Systems;

public sealed class DebrainedMovementSystem : EntitySystem
{
    [Dependency] private ActionBlockerSystem _blocker = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DebrainedComponent, UpdateCanMoveEvent>(OnMoveAttempt);
        SubscribeLocalEvent<DebrainedComponent, ComponentStartup>(OnChanged);
        SubscribeLocalEvent<DebrainedComponent, ComponentShutdown>(OnChanged);
    }

    private void OnMoveAttempt(EntityUid uid, DebrainedComponent comp, UpdateCanMoveEvent args)
    {
        if (comp.LifeStage > ComponentLifeStage.Running)
            return;

        args.Cancel();
    }

    private void OnChanged<T>(Entity<DebrainedComponent> ent, ref T args)
    {
        _blocker.UpdateCanMove(ent);
    }
}