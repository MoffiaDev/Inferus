// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - what an EMP does to cybernetic organs. Organs inside a body are not found by the pulse, only the body is, so the body
// passes the pulse on to every machine organ it carries. A disabled organ counts as lost to the body, like a failed one, until the
// EMP wears off.
using System.Linq;
using Content.Shared._Shitmed.Surgery.Wounds.Systems;
using Content.Shared.Body;
using Content.Shared.Emp;
using Content.Shared.Movement.Systems; // Inferus
using Robust.Shared.Network;

namespace Content.Shared._Shitmed.Surgery.Organs;

public sealed partial class CyberneticOrganSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private OrganDamageSystem _organDamage = default!;
    [Dependency] private SharedEmpSystem _emp = default!;
    [Dependency] private WoundSystem _wounds = default!;
    [Dependency] private HandOrganSystem _handOrgan = default!; // Inferus
    [Dependency] private OrganRelationSystem _organRelation = default!; // Inferus
    [Dependency] private MovementSpeedModifierSystem _movement = default!; // Inferus

    /// <summary>
    /// The pulse hits the body, which hands it on to its machine organs.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBodyEmp(Entity<BodyComponent> body, ref EmpPulseEvent args)
    {
        foreach (var organ in _body.EnumerateOrgans<CyberneticOrganComponent>(body.Owner).ToList())
        {
            _emp.DoEmpEffects(organ.Owner, args.EnergyConsumption, args.Duration, args.User);
        }
    }

    [SubscribeLocalEvent]
    private void OnOrganEmp(Entity<CyberneticOrganComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;

        if (_net.IsClient)
            return;

        if (TryComp<OrganDamageComponent>(ent, out var damage))
            _organDamage.ApplyDamage((ent.Owner, damage), ent.Comp.EmpDamage);

        if (ent.Comp.MarkLost && TryComp<OrganComponent>(ent, out var organ) && organ.Body is { } body && organ.Category is { } category) // Inferus
            _wounds.SetOrganLost(body, category, true);

        SetHands(ent.Owner, true); // Inferus
        SetLeg(ent, true); // Inferus
    }

    /// <summary>
    /// The EMP is over, the organ works again unless it has failed in the meantime.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnEmpOver(Entity<CyberneticOrganComponent> ent, ref EmpDisabledRemovedEvent args)
    {
        if (_net.IsClient)
            return;

        SetHands(ent.Owner, false); // Inferus
        SetLeg(ent, false); // Inferus

        if (!ent.Comp.MarkLost
            || !TryComp<OrganComponent>(ent, out var organ)
            || organ.Body is not { } body
            || organ.Category is not { } category
            || TryComp<OrganDamageComponent>(ent, out var damage) && damage.Severity == OrganSeverity.Failed)
            return;

        _wounds.SetOrganLost(body, category, false);
    }

    /// <summary>
    /// Takes the hands of a switched off limb from the body, or gives them back. That is the hand of the organ itself and every hand below it,
    /// so an arm that is off leaves its hand unable to hold anything.
    /// </summary>
    private void SetHands(EntityUid organ, bool disabled)
    {
        if (!TryComp<OrganComponent>(organ, out var organComp) || organComp.Body is not { } body)
            return;

        SetHand(organ, body, disabled);
        foreach (var child in _organRelation.AllChildren(organ))
        {
            SetHand(child.Owner, body, disabled);
        }
    }

    private void SetHand(EntityUid uid, EntityUid body, bool disabled)
    {
        if (!TryComp<HandOrganComponent>(uid, out var hand) || disabled == HasComp<EmpDisabledHandComponent>(uid))
            return;

        if (disabled)
            EnsureComp<EmpDisabledHandComponent>(uid);
        else
            RemComp<EmpDisabledHandComponent>(uid);

        _handOrgan.SetEnabled((uid, hand), body, !disabled);
    }

    /// <summary>
    /// A machine organ that was put into a body while an EMP still has it switched off. The body does not get the use of its hand and
    /// is slowed by its leg until the EMP wears off, just as if the pulse had hit it there.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnInserted(Entity<CyberneticOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        if (_net.IsClient || !HasComp<EmpDisabledComponent>(ent))
            return;

        MarkHands(ent.Owner);
        SetLeg(ent, true);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CyberneticOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (_net.IsClient || !HasComp<EmpDisabledLegComponent>(ent))
            return;

        RemComp<EmpDisabledLegComponent>(ent);
        if (!TerminatingOrDeleted(args.Target))
            _movement.RefreshMovementSpeedModifiers(args.Target);
    }

    /// <summary>
    /// Marks the hands of an organ as off without touching the hands of the body: the hand organ does not give its hand to a body while an EMP has it.
    /// </summary>
    private void MarkHands(EntityUid organ)
    {
        if (HasComp<HandOrganComponent>(organ))
            EnsureComp<EmpDisabledHandComponent>(organ);

        foreach (var child in _organRelation.AllChildren(organ))
        {
            if (HasComp<HandOrganComponent>(child.Owner))
                EnsureComp<EmpDisabledHandComponent>(child.Owner);
        }
    }

    private void SetLeg(Entity<CyberneticOrganComponent> ent, bool disabled)
    {
        if (!TryComp<OrganComponent>(ent, out var organ)
            || organ.Category?.Id is not { } category
            || !category.StartsWith("Leg") && !category.StartsWith("Foot"))
            return;

        if (disabled)
            EnsureComp<EmpDisabledLegComponent>(ent);
        else
            RemComp<EmpDisabledLegComponent>(ent);

        if (organ.Body is { } body)
            _movement.RefreshMovementSpeedModifiers(body);
    }

    /// <summary>
    /// Every leg an EMP has switched off slows the body down.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRefreshSpeed(Entity<BodyComponent> body, ref RefreshMovementSpeedModifiersEvent args)
    {
        var modifier = 1f;
        foreach (var leg in _body.EnumerateOrgans<EmpDisabledLegComponent>(body.Owner))
        {
            if (TryComp<CyberneticOrganComponent>(leg, out var cybernetic))
                modifier *= cybernetic.EmpSpeedModifier;
        }

        if (modifier < 1f)
            args.ModifySpeed(modifier);
    }
}
