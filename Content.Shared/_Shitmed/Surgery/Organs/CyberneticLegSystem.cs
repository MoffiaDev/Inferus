// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (SharedBodySystem.UpdateMovementSpeed, MovementHinderedByShoesSystem) // Inferus
// Inferus - the speed of a body with cybernetic legs. Every leg counts for its share, a leg without the component is an organic one and counts
// as normal speed, like in Starlight where the speed of the body is the average of its legs. Wearing shoes takes sprint speed away,
// depending on the legs.
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;
using Content.Shared.Inventory;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Shared._Shitmed.Surgery.Organs;

public sealed partial class CyberneticLegSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MovementSpeedModifierSystem _movement = default!;

    // Inferus - on the movement component instead of the body, the cybernetic organ system already handles the speed event of the body
    [SubscribeLocalEvent]
    private void OnRefreshSpeed(Entity<MovementSpeedModifierComponent> body, ref RefreshMovementSpeedModifiersEvent args)
    {
        var legs = 0;
        var walk = 0f;
        var sprint = 0f;
        var hinder = 0f;
        var cybernetic = false;

        foreach (var limb in _body.EnumerateOrgans<WoundableComponent>(body.Owner))
        {
            if (limb.Comp1.Category?.Id is not { } category || !category.StartsWith("Leg"))
                continue;

            legs++;
            if (TryComp<CyberneticLegComponent>(limb, out var leg))
            {
                cybernetic = true;
                walk += leg.WalkModifier;
                sprint += leg.SprintModifier;
                hinder += leg.ShoeHinder;
            }
            else
            {
                walk += 1f;
                sprint += 1f;
            }
        }

        if (!cybernetic || legs == 0)
            return;

        var sprintMod = sprint / legs;
        if (hinder > 0f && _inventory.TryGetSlotEntity(body.Owner, "shoes", out _))
            sprintMod *= Math.Max(0f, 1f - hinder);

        args.ModifySpeed(walk / legs, sprintMod);
    }

    /// <summary>
    /// A leg that comes or goes changes the speed.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnInserted(Entity<CyberneticLegComponent> ent, ref OrganGotInsertedEvent args)
    {
        _movement.RefreshMovementSpeedModifiers(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnRemoved(Entity<CyberneticLegComponent> ent, ref OrganGotRemovedEvent args)
    {
        if (!TerminatingOrDeleted(args.Target))
            _movement.RefreshMovementSpeedModifiers(args.Target);
    }
}
