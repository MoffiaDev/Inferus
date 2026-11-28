// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (Content.Shared/_Starlight/Storage/SharedPrivateStorageSystem.cs) // Inferus
// Inferus - others open the storage organ of a body, like a cargo leg, from the strip menu. They wait first, and the owner is told who it is.
// The owner uses the action of the organ and needs none of this.
using Content.Shared.Administration.Logs;
using Content.Shared.Body;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Strip;
using Content.Shared.Strip.Components;
using Robust.Shared.Network;

namespace Content.Shared._Shitmed.Surgery.Organs;

public sealed partial class OrganStorageAccessSystem : EntitySystem
{
    private static readonly TimeSpan AccessDelay = TimeSpan.FromSeconds(3);
    private const string AccessPopup = "organ-storage-accessing";

    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedInteractionSystem _interaction = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [SubscribeLocalEvent]
    private void OnButtonPressed(Entity<StrippableComponent> target, ref StrippingOrganStorageButtonPressed args)
    {
        var user = args.Actor;
        var organ = GetEntity(args.Organ);

        if (args.UiKey is not StrippingUiKey.Key
            || !user.IsValid()
            || !CanAccess(target.Owner, user, organ))
            return;

        // A second press closes it
        if (_ui.IsUiOpen(organ, StorageComponent.StorageUiKey.Key, user))
        {
            _ui.CloseUi(organ, StorageComponent.StorageUiKey.Key, user);
            return;
        }

        if (user == target.Owner)
        {
            Open(organ, user);
            return;
        }

        var ev = new BeforeStripEvent(AccessDelay);
        RaiseLocalEvent(user, ref ev);

        var doAfterArgs = new DoAfterArgs(EntityManager, user, ev.Time, new OrganStorageDoAfterEvent(), target.Owner, target: target.Owner, used: organ)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            NeedHand = false,
            RequireCanInteract = true,
            BlockDuplicate = true,
            CancelDuplicate = true,
            Hidden = ev.Stealth,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        if (!ev.Stealth)
        {
            _popup.PopupEntity(
                Loc.GetString(AccessPopup, ("user", user), ("organ", organ)),
                target.Owner,
                target.Owner,
                PopupType.Medium);
        }

        _adminLogger.Add(LogType.Stripping, LogImpact.Low,
            $"{ToPrettyString(user):actor} is trying to open the storage {ToPrettyString(organ):organ} of {ToPrettyString(target.Owner):target}");
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(EntityUid uid, StrippableComponent comp, OrganStorageDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Args.Used is not { } organ)
            return;

        if (!CanAccess(uid, args.Args.User, organ))
            return;

        Open(organ, args.Args.User);
        args.Handled = true;
    }

    /// <summary>
    /// Whoever has the strip menu of the body open and stands next to it can reach the storage of its organs.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnStorageRangeCheck(Entity<OrganComponent> ent, ref BoundUserInterfaceCheckRangeEvent args)
    {
        if (args.UiKey is not StorageComponent.StorageUiKey.Key
            || !HasComp<OrganStorageComponent>(ent)
            || ent.Comp.Body is not { } body
            || !HasComp<StrippableComponent>(body)
            || !_ui.IsUiOpen(body, StrippingUiKey.Key, args.Actor.Owner))
            return;

        args.Result = args.Actor.Owner == body
                      || _interaction.InRangeUnobstructed(args.Actor.Owner, body, args.Data.InteractionRange)
            ? BoundUserInterfaceRangeResult.Pass
            : BoundUserInterfaceRangeResult.Fail;
    }

    private bool CanAccess(EntityUid body, EntityUid user, EntityUid organ)
    {
        if (!HasComp<OrganStorageComponent>(organ)
            || !HasComp<StorageComponent>(organ)
            || !TryComp<OrganComponent>(organ, out var organComp)
            || organComp.Body != body)
            return false;

        if (!_ui.IsUiOpen(body, StrippingUiKey.Key, user))
            return false;

        return user == body || _interaction.InRangeUnobstructed(user, body);
    }

    private void Open(EntityUid organ, EntityUid user)
    {
        // Opening the storage is up to the server, the same as for the action of the organ
        if (_net.IsClient || !TryComp<StorageComponent>(organ, out var storage))
            return;

        _storage.OpenStorageUI(organ, user, storage, false);
    }
}
