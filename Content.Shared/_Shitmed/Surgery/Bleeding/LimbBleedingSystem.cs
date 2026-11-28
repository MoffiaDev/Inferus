// SPDX-License-Identifier: AGPL-3.0-or-later
// bleeding is worked out per limb from its wounds. A tourniquet on a limb takes that limb's share out of the body's bleeding.
// The body keeps using the bloodstream's own bleed rate, this only changes how much of it leaks each tick.
using System.Linq;
using Content.Shared._Shitmed.Surgery.Operations; // Inferus
using Content.Shared._Shitmed.Surgery.Pain;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Surgery.Wounds.Systems;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body;
using Content.Shared.Body.Events;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Bleeding;

public sealed partial class LimbBleedingSystem : EntitySystem
{
    /// <summary>
    /// How much of a wound's severity counts as bleeding, by damage type. A type that is not listed does not bleed.
    /// </summary>
    private static readonly Dictionary<string, float> BleedFactors = new()
    {
        { "Slash", 1f },
        { "Piercing", 0.8f },
        { "Blunt", 0.2f },
    };

    /// <summary>
    /// A limb with at least this much bleeding is reported as bleeding.
    /// </summary>
    private const float BleedingThreshold = 2f;

    /// <summary>
    /// What a limb with an open incision counts as bleeding, until the vessels are clamped. // Inferus
    /// </summary>
    private const float OpenIncisionBleed = 3f;

    [Dependency] private INetManager _net = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private WoundSystem _wound = default!;
    [Dependency] private PainSystem _pain = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    #region Bleeding

    /// <summary>
    /// How much a limb is bleeding, from its open wounds.
    /// </summary>
    public float GetLimbBleed(EntityUid limb, WoundableComponent? woundable = null)
    {
        if (!Resolve(limb, ref woundable, false))
            return 0f;

        var bleed = 0f;
        foreach (var wound in _wound.GetWoundableWounds(limb, woundable))
        {
            if (wound.Comp.IsScar || !BleedFactors.TryGetValue(wound.Comp.DamageType, out var factor))
                continue;

            bleed += wound.Comp.WoundSeverityPoint.Float() * factor;
        }

        // Inferus - an open incision bleeds until the vessels are clamped
        if (HasComp<IncisionOpenComponent>(limb) && !HasComp<BleedersClampedComponent>(limb))
            bleed += OpenIncisionBleed;

        return bleed;
    }

    public bool IsBleeding(EntityUid limb, WoundableComponent? woundable = null)
    {
        return GetLimbBleed(limb, woundable) >= BleedingThreshold;
    }

    /// <summary>
    /// The bloodstream asks every tick how much of its bleeding really leaks. Tourniqueted limbs do not leak.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBleedModifier(Entity<WoundDamageModelComponent> body, ref BleedModifierEvent args)
    {
        var total = 0f;
        var blocked = 0f;

        foreach (var limb in _body.EnumerateOrgans<WoundableComponent>(body.Owner))
        {
            var bleed = GetLimbBleed(limb.Owner, limb.Comp2);
            total += bleed;

            if (HasComp<TourniquetedComponent>(limb.Owner))
                blocked += bleed;
        }

        if (total <= 0f || blocked <= 0f)
            return;

        args.BleedAmount *= 1f - Math.Min(1f, blocked / total);
    }

    #endregion

    #region Applying

    [SubscribeLocalEvent]
    private void OnAfterInteract(Entity<TourniquetComponent> item, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (TryStart(item, args.User, target))
            args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnUseInHand(Entity<TourniquetComponent> item, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (TryStart(item, args.User, args.User))
            args.Handled = true;
    }

    private bool TryStart(Entity<TourniquetComponent> item, EntityUid user, EntityUid target)
    {
        if (!HasComp<WoundDamageModelComponent>(target)
            || !TryComp<TargetingComponent>(user, out var targeting))
            return false;

        if (GetTourniquetLimb(targeting.Target) is not { } category)
        {
            _popup.PopupClient(Loc.GetString("shitmed-tourniquet-bad-target"), target, user);
            return true;
        }

        if (FindLimb(target, category) is not { } limb)
        {
            _popup.PopupClient(Loc.GetString("shitmed-tourniquet-no-limb"), target, user);
            return true;
        }

        if (HasComp<TourniquetedComponent>(limb))
        {
            _popup.PopupClient(Loc.GetString("shitmed-tourniquet-already", ("limb", Name(limb))), target, user);
            return true;
        }

        var ev = new TourniquetDoAfterEvent { Limb = category.Id };
        var doAfter = new DoAfterArgs(EntityManager, user, item.Comp.Delay, ev, item, target: target, used: item)
        {
            NeedHand = true,
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
        return true;
    }

    [SubscribeLocalEvent]
    private void OnTourniquetDoAfter(Entity<TourniquetComponent> item, ref TourniquetDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        args.Handled = true;

        if (_net.IsClient)
            return;

        var category = new ProtoId<OrganCategoryPrototype>(args.Limb);
        if (FindLimb(target, category) is not { } limb || HasComp<TourniquetedComponent>(limb))
            return;

        var tourniquet = EnsureComp<TourniquetedComponent>(limb);
        tourniquet.Item = MetaData(item).EntityPrototype?.ID;
        Dirty(limb, tourniquet);

        var pain = EnsureComp<PainComponent>(target);
        _pain.AddPain((target, pain), item.Comp.Pain);

        _popup.PopupEntity(Loc.GetString("shitmed-tourniquet-applied", ("limb", Name(limb))), target, args.User);
        QueueDel(item);
    }

    #endregion

    #region Removing

    [SubscribeLocalEvent]
    private void OnGetVerbs(Entity<WoundDamageModelComponent> body, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;
        var bodyUid = body.Owner;

        foreach (var limb in _body.EnumerateOrgans<TourniquetedComponent>(bodyUid).ToList())
        {
            var limbUid = limb.Owner;
            args.Verbs.Add(new InteractionVerb
            {
                Text = Loc.GetString("shitmed-tourniquet-remove", ("limb", Name(limbUid))),
                Act = () => RemoveTourniquet(limbUid, bodyUid, user),
                Priority = 2,
            });
        }
    }

    private void RemoveTourniquet(EntityUid limb, EntityUid body, EntityUid user)
    {
        if (_net.IsClient || !TryComp<TourniquetedComponent>(limb, out var tourniquet))
            return;

        var item = tourniquet.Item;
        RemComp<TourniquetedComponent>(limb);

        if (item != null)
            SpawnNextToOrDrop(item.Value, user);

        _popup.PopupEntity(Loc.GetString("shitmed-tourniquet-removed", ("limb", Name(limb))), body, user);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// The arm or leg a tourniquet goes on when this part is targeted. Hands count as their arm, feet as their leg.
    /// </summary>
    private static ProtoId<OrganCategoryPrototype>? GetTourniquetLimb(TargetBodyPart part)
    {
        return part switch
        {
            TargetBodyPart.LeftArm or TargetBodyPart.LeftHand => "ArmLeft",
            TargetBodyPart.RightArm or TargetBodyPart.RightHand => "ArmRight",
            TargetBodyPart.LeftLeg or TargetBodyPart.LeftFoot => "LegLeft",
            TargetBodyPart.RightLeg or TargetBodyPart.RightFoot => "LegRight",
            _ => null,
        };
    }

    private EntityUid? FindLimb(EntityUid body, ProtoId<OrganCategoryPrototype> category)
    {
        foreach (var limb in _body.EnumerateOrgans<WoundableComponent>(body))
        {
            if (limb.Comp1.Category == category)
                return limb.Owner;
        }

        return null;
    }

    #endregion
}
