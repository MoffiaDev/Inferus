// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - lets the traumas on a wound hold its natural healing back.
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;

namespace Content.Shared._Shitmed.Surgery.Wounds.Systems;

public sealed partial class WoundSystem
{
    private bool IsHealBlocked(Entity<OrganComponent, WoundableComponent> limb, Entity<WoundComponent> wound)
    {
        var ev = new WoundHealAttemptEvent((limb.Owner, limb.Comp2));
        RaiseLocalEvent(wound.Owner, ref ev);
        return ev.Cancelled;
    }
}