// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (Content.Server/_Starlight/Medical/Limbs/CyberLimbSystem.LimbWithItems.cs, LimbItemStorageSystem.cs) // Inferus
// Inferus - the tools of a cybernetic hand. They live in a container of the hand organ. Toggling gives the body one extra hand for each tool and
// puts the tool in it, toggling again puts the tools back. An EMP or losing the hand puts them back as well.
using Content.Shared._Shitmed.Surgery.Organs;
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Emp;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;

namespace Content.Server._Shitmed.Surgery.Organs;

public sealed partial class LimbItemDeployerSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;

    [SubscribeLocalEvent]
    private void OnStorageInit(Entity<LimbItemStorageComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.ItemEntities.Count == ent.Comp.Items.Count)
            return;

        var container = _container.EnsureContainer<Container>(ent.Owner, ent.Comp.ContainerId);
        foreach (var proto in ent.Comp.Items)
        {
            var item = Spawn(proto);
            _container.Insert(item, container, force: true);
            ent.Comp.ItemEntities.Add(item);
        }
    }

    /// <summary>
    /// The body gets the action of the hand while it has the hand.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnInserted(Entity<LimbItemDeployerComponent> ent, ref OrganGotInsertedEvent args)
    {
        ent.Comp.ActionEntity = _actions.AddAction(args.Target, ent.Comp.Action);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<LimbItemDeployerComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (ent.Comp.Toggled)
            Retract(ent.Owner, ent.Comp, args.Target);

        if (ent.Comp.ActionEntity is { } action)
            _actions.RemoveAction(action);

        ent.Comp.ActionEntity = null;
    }

    [SubscribeLocalEvent]
    private void OnEmp(Entity<LimbItemDeployerComponent> ent, ref EmpPulseEvent args)
    {
        if (ent.Comp.Toggled && TryComp<OrganComponent>(ent, out var organ) && organ.Body is { } body)
            Retract(ent.Owner, ent.Comp, body);
    }

    [SubscribeLocalEvent]
    private void OnToggle(Entity<HandsComponent> ent, ref ToggleLimbEvent args)
    {
        foreach (var organ in _body.EnumerateOrgans<LimbItemDeployerComponent>(ent.Owner))
        {
            if (organ.Comp1.Category != args.Category)
                continue;

            args.Handled = true;

            if (organ.Comp2.Toggled)
                Retract(organ.Owner, organ.Comp2, ent.Owner);
            else if (!HasComp<EmpDisabledComponent>(organ.Owner)) // a switched off hand cannot put its tools out
                Deploy(organ.Owner, organ.Comp2, ent.Owner);

            _audio.PlayPvs(organ.Comp2.Sound, ent.Owner);
            return;
        }
    }

    private void Deploy(EntityUid organ, LimbItemDeployerComponent deployer, EntityUid user)
    {
        if (!TryComp<LimbItemStorageComponent>(organ, out var storage))
            return;

        foreach (var item in storage.ItemEntities)
        {
            if (TerminatingOrDeleted(item))
                continue;

            var handId = HandId(organ, item);
            _hands.AddHand(user, handId, HandLocation.Middle, whitelist: deployer.HandWhitelist);
            _hands.TryPickup(user, item, handId, checkActionBlocker: false);
            EnsureComp<UnremoveableComponent>(item);
        }

        deployer.Toggled = true;
    }

    private void Retract(EntityUid organ, LimbItemDeployerComponent deployer, EntityUid user)
    {
        deployer.Toggled = false;

        if (!TryComp<LimbItemStorageComponent>(organ, out var storage))
            return;

        var container = _container.EnsureContainer<Container>(organ, storage.ContainerId);
        foreach (var item in storage.ItemEntities)
        {
            if (TerminatingOrDeleted(item))
                continue;

            RemComp<UnremoveableComponent>(item);
            _container.Insert(item, container, force: true);
            _hands.RemoveHand(user, HandId(organ, item));
        }
    }

    private static string HandId(EntityUid organ, EntityUid item)
    {
        return $"{organ}_{item}";
    }
}
