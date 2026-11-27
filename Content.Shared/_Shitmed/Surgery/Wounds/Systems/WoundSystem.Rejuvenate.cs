// SPDX-License-Identifier: AGPL-3.0-or-later
// rejuvenate zeroes the body's damage, but the damage lives in wounds, so they have to be cleared too.
using System.Linq;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;
using Content.Shared.Rejuvenate;

namespace Content.Shared._Shitmed.Surgery.Wounds.Systems;

public sealed partial class WoundSystem
{
    [SubscribeLocalEvent]
    private void OnRejuvenate(Entity<WoundDamageModelComponent> ent, ref RejuvenateEvent args)
    {
        if (_net.IsClient)
            return;

        ent.Comp.BodyDamage.DamageDict.Clear();

        var woundables = _body.EnumerateOrgans<WoundableComponent>(ent.Owner).ToList();
        foreach (var limb in woundables)
        {
            foreach (var wound in GetWoundableWounds(limb, limb.Comp2).ToList())
            {
                RemoveWound(wound, wound.Comp);
            }

            UpdateWoundableIntegrity(limb, limb.Comp2);
            CheckWoundableSeverityThresholds(limb, limb.Comp2);
        }

        SyncBodyDamage(ent, woundables);
    }
}
