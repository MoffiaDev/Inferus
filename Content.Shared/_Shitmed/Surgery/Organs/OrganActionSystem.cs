// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (SharedJumpSystem, the storage organ code of SharedStorageSystem and SharedPrivateStorageSystem) // Inferus
// Inferus - the actions of the pilebunker and cargo legs. The server does the jump and opens the storage, both only for the body that owns the organ.
using System.Numerics;
using Content.Shared.Body;
using Content.Shared.Emp;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Throwing;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Content.Shared.Actions;

namespace Content.Shared._Shitmed.Surgery.Organs;

public sealed partial class OrganActionSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private ThrowingSystem _throwing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [SubscribeLocalEvent]
    private void OnInserted(Entity<OrganActionComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_net.IsServer)
            ent.Comp.ActionEntity = _actions.AddAction(args.Target, ent.Comp.Action);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<OrganActionComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!_net.IsServer)
            return;

        if (ent.Comp.ActionEntity is { } action)
            _actions.RemoveAction(action);

        ent.Comp.ActionEntity = null;
    }

    [SubscribeLocalEvent]
    private void OnJump(Entity<BodyComponent> ent, ref OrganJumpEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        var working = false;
        foreach (var organ in _body.EnumerateOrgans<OrganJumpComponent>(ent.Owner))
        {
            if (HasComp<EmpDisabledComponent>(organ.Owner))
                continue;

            working = true;
            break;
        }

        if (!working)
            return;

        var user = ent.Owner;
        var from = _transform.GetMapCoordinates(user).Position;
        var to = _transform.ToMapCoordinates(args.Target).Position;
        var vector = to - from;
        if (vector.LengthSquared() < 0.0001f)
            return;

        if (!args.ToPointer || vector.Length() > args.Distance)
            vector = Vector2.Normalize(vector) * args.Distance;

        _throwing.TryThrow(user, vector, baseThrowSpeed: args.Speed, doSpin: false);

        if (args.Sound != null)
            _audio.PlayPvs(args.Sound, user, AudioParams.Default.WithVolume(-4f));

        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnOpenStorage(Entity<BodyComponent> ent, ref OpenOrganStorageEvent args)
    {
        if (args.Handled || _net.IsClient)
            return;

        foreach (var organ in _body.EnumerateOrgans<OrganStorageComponent>(ent.Owner))
        {
            if (organ.Comp1.Category != args.Category || !TryComp<StorageComponent>(organ.Owner, out var storage))
                continue;

            _storage.OpenStorageUI(organ.Owner, ent.Owner, storage, false);
            args.Handled = true;
            return;
        }
    }

    /// <summary>
    /// The storage of an organ is reachable for the body that has the organ, however it sits inside that body.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnStorageRangeCheck(Entity<OrganStorageComponent> ent, ref BoundUserInterfaceCheckRangeEvent args)
    {
        if (args.UiKey is not StorageComponent.StorageUiKey.Key
            || !TryComp<OrganComponent>(ent, out var organ)
            || organ.Body != args.Actor.Owner)
            return;

        args.Result = BoundUserInterfaceRangeResult.Pass;
    }
}
