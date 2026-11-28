// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - what a body loses when an organ is taken out: vital organs add shock (and so can kill), missing eyes blind,
// and the respirator reads the list for missing lungs.
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Wounds.Systems;

public sealed partial class WoundSystem
{
    private static readonly ProtoId<OrganCategoryPrototype> EyesCategory = "Eyes";

    [Dependency] private BlindableSystem _blindable = default!;

    [SubscribeLocalEvent]
    private void OnOrganRemoved(Entity<WoundDamageModelComponent> ent, ref OrganRemovedFromEvent args)
    {
        if (TerminatingOrDeleted(ent.Owner)
            || !TryComp<OrganComponent>(args.Organ, out var organ)
            || organ.Category is not { } category)
            return;

        var lost = EnsureComp<LostOrgansComponent>(ent.Owner);
        if (!lost.Categories.Add(category))
            return;

        Dirty(ent.Owner, lost);
        OnLostOrgansChanged(ent);
    }

    [SubscribeLocalEvent]
    private void OnOrganInserted(Entity<WoundDamageModelComponent> ent, ref OrganInsertedIntoEvent args)
    {
        if (TerminatingOrDeleted(ent.Owner)
            || !TryComp<LostOrgansComponent>(ent.Owner, out var lost)
            || !TryComp<OrganComponent>(args.Organ, out var organ)
            || organ.Category is not { } category
            || !lost.Categories.Remove(category))
            return;

        Dirty(ent.Owner, lost);
        OnLostOrgansChanged(ent);
    }

    private void OnLostOrgansChanged(Entity<WoundDamageModelComponent> body)
    {
        _blindable.UpdateIsBlind(body.Owner);

        if (!_net.IsClient)
            RefreshBodyDamage(body.Owner);
    }

    [SubscribeLocalEvent]
    private void OnCanSeeAttempt(EntityUid uid, LostOrgansComponent comp, CanSeeAttemptEvent args)
    {
        if (comp.Categories.Contains(EyesCategory))
            args.Cancel();
    }

    /// <summary>
    /// Shock for every vital organ that was taken out of the body.
    /// </summary>
    private FixedPoint2 GetLostOrganShock(
        WoundDamageModelComponent model,
        List<Entity<OrganComponent, WoundableComponent>> woundables)
    {
        if (woundables.Count == 0
            || woundables[0].Comp1.Body is not { } body
            || !TryComp<LostOrgansComponent>(body, out var lost))
            return FixedPoint2.Zero;

        var shock = FixedPoint2.Zero;
        foreach (var organ in model.VitalOrgans)
        {
            if (lost.Categories.Contains(organ))
                shock += model.VitalOrganDamage;
        }

        return shock;
    }
}
