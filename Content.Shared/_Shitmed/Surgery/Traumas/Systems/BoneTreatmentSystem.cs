// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - applying a bone treatment item to the limb the user has targeted.
using Content.Shared._Shitmed.Surgery.Pain;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Traumas.Systems;

public sealed partial class BoneTreatmentSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BoneSystem _bone = default!;
    [Dependency] private PainSystem _pain = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<BoneTreatmentComponent> item, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (TryStart(item, args.User, target))
            args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<BoneTreatmentComponent> item, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryStart(item, args.User, args.User))
            args.Handled = true;
    }

    private bool TryStart(Entity<BoneTreatmentComponent> item, EntityUid user, EntityUid target)
    {
        if (!HasComp<WoundDamageModelComponent>(target)
            || !TryComp<TargetingComponent>(user, out var targeting))
            return false;

        var category = SharedTargetingSystem.ToOrganCategory(targeting.Target);
        if (FindBone(target, category) is not { } limb)
        {
            _popup.PopupClient(Loc.GetString("shitmed-bone-treatment-no-limb"), target, user);
            return true;
        }

        if (limb.Comp.BoneSeverity < item.Comp.MinimumSeverity)
        {
            _popup.PopupClient(Loc.GetString("shitmed-bone-treatment-not-needed", ("limb", Name(limb))), target, user);
            return true;
        }

        var ev = new BoneTreatmentDoAfterEvent { Limb = category.Id };
        var doAfter = new DoAfterArgs(EntityManager, user, item.Comp.Delay, ev, item, target: target, used: item)
        {
            NeedHand = true,
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
        return true;
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<BoneTreatmentComponent> item, ref BoneTreatmentDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        var category = new ProtoId<OrganCategoryPrototype>(args.Limb);
        if (FindBone(target, category) is not { } limb || limb.Comp.BoneSeverity < item.Comp.MinimumSeverity)
            return;

        _bone.SetIntegrity(limb, limb.Comp.BoneIntegrity + item.Comp.Integrity);

        var pain = EnsureComp<PainComponent>(target);
        _pain.AddPain((target, pain), item.Comp.Pain);

        _popup.PopupEntity(Loc.GetString("shitmed-bone-treatment-done", ("limb", Name(limb))), target, args.User);
        QueueDel(item);
    }

    private Entity<BoneComponent>? FindBone(EntityUid body, ProtoId<OrganCategoryPrototype> category)
    {
        foreach (var limb in _body.EnumerateOrgans<BoneComponent>(body))
        {
            if (limb.Comp1.Category == category)
                return (limb.Owner, limb.Comp2);
        }

        return null;
    }
}
