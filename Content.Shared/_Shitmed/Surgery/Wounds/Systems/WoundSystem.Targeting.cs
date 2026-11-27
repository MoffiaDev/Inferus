// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - keeps the per-part wound status on TargetingComponent up to date, the targeting doll shows it.
using System.Linq;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body;

namespace Content.Shared._Shitmed.Surgery.Wounds.Systems;

public sealed partial class WoundSystem
{
    [Dependency] private EntityQuery<TargetingComponent> _targetingQuery = default!;

    /// <summary>
    /// Works out the severity of every targetable part from the body's limbs and stores it on the body's <see cref="TargetingComponent"/>.
    /// A part with no limb in the body is reported as severed. Torso covers both the chest and the groin.
    /// </summary>
    private void UpdateBodyStatus(EntityUid body, List<Entity<OrganComponent, WoundableComponent>> woundables)
    {
        if (!_targetingQuery.TryComp(body, out var targeting))
            return;

        var status = new Dictionary<TargetBodyPart, WoundableSeverity>();
        foreach (var part in SharedTargetingSystem.GetValidParts())
        {
            var category = SharedTargetingSystem.ToOrganCategory(part);
            status[part] = WoundableSeverity.Severed;

            foreach (var limb in woundables)
            {
                if (limb.Comp1.Category != category)
                    continue;

                status[part] = limb.Comp2.WoundableSeverity;
                break;
            }
        }

        if (status.Count == targeting.BodyStatus.Count
            && status.All(pair => targeting.BodyStatus.TryGetValue(pair.Key, out var old) && old == pair.Value))
            return;

        targeting.BodyStatus = status;
        Dirty(body, targeting);
    }
}
