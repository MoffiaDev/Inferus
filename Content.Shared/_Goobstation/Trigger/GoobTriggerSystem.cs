// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Misandry <mary@thughunt.ing>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Trigger;
using Content.Shared.Ensnaring;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Stunnable;
using Content.Shared.Trigger;

namespace Content.Shared._Goobstation.Trigger;

public sealed class GoobTriggerSystem : EntitySystem
{
    [Dependency] private SharedEnsnareableSystem _snare = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ParalyzePullerOnTriggerComponent, TriggerEvent>(OnParalyzeTrigger);
        SubscribeLocalEvent<RemoveSnareOnTriggerComponent, TriggerEvent>(OnSnareTrigger);
    }

    private void OnSnareTrigger(Entity<RemoveSnareOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (!TryComp<EnsnareableComponent>(target, out var ensnareable)
            || ensnareable.Container is not { } container
            || container.ContainedEntities.Count <= 0) // Inferus: no IsEnsnared property
            return;

        var bola = container.ContainedEntities[0];
        _snare.ForceFree(bola);
        args.Handled = true;
    }

    private void OnParalyzeTrigger(Entity<ParalyzePullerOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (!TryComp<PullableComponent>(target, out var pullable) || !pullable.Puller.HasValue)
            return;

        _stun.TryUpdateParalyzeDuration(pullable.Puller.Value, ent.Comp.ParalyzeTime);
        args.Handled = true;
    }
}
