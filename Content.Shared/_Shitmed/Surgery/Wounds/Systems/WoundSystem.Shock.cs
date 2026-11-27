// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - shock damage from badly hurt or missing limbs. It is worked out from the limbs every time the body's damage is synced,
// so it goes away on its own when the limb is treated, and it never sits in the body's own damage.
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Wounds.Systems;

public sealed partial class WoundSystem
{
    private void AddShockDamage(
        WoundDamageModelComponent model,
        List<Entity<OrganComponent, WoundableComponent>> woundables,
        DamageSpecifier total)
    {
        var shock = FixedPoint2.Zero;
        var present = new HashSet<ProtoId<OrganCategoryPrototype>>();

        foreach (var limb in woundables)
        {
            var severity = limb.Comp2.WoundableSeverity;

            if (model.SeverityDamage.TryGetValue(severity, out var pain))
                shock += pain;

            if (limb.Comp1.Category is not { } category)
                continue;

            present.Add(category);

            if (model.VitalLimbs.Contains(category) && model.VitalSeverityDamage.TryGetValue(severity, out var vital))
                shock += vital;
        }

        // A vital limb that is gone is as bad as one that is severed.
        if (model.VitalSeverityDamage.TryGetValue(WoundableSeverity.Severed, out var lost))
        {
            foreach (var vitalCategory in model.VitalLimbs)
            {
                if (!present.Contains(vitalCategory))
                    shock += lost;
            }
        }

        if (shock <= 0)
            return;

        var type = (string) model.ShockDamageType;
        total.DamageDict[type] = total.DamageDict.GetValueOrDefault(type) + shock;
    }
}
