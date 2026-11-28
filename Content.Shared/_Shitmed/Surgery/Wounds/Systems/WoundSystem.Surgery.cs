// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - the parts of the wound system that surgery needs: cutting a limb, tending its wounds, and taking a severed limb back.
using System.Linq;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;
using Content.Shared.FixedPoint;

namespace Content.Shared._Shitmed.Surgery.Wounds.Systems;

public sealed partial class WoundSystem
{
    /// <summary>
    /// How much wound severity of these damage types a limb holds.
    /// </summary>
    public FixedPoint2 GetLimbWounds(EntityUid limb, IReadOnlyCollection<string> types)
    {
        var total = FixedPoint2.Zero;
        foreach (var wound in GetWoundableWounds(limb))
        {
            if (!wound.Comp.IsScar && types.Contains((string) wound.Comp.DamageType))
                total += wound.Comp.WoundSeverityPoint;
        }

        return total;
    }

    /// <summary>
    /// Wounds a specific limb, as a hit on it would.
    /// </summary>
    public bool TryDamageLimb(EntityUid limb, string type, FixedPoint2 amount)
    {
        if (_net.IsClient
            || amount <= 0
            || !_woundableQuery.TryComp(limb, out var woundable)
            || !TryComp<OrganComponent>(limb, out var organ)
            || organ.Body is not { } body
            || !TryInduceWound(limb, type, amount, out _, woundable))
            return false;

        var damaged = new WoundableDamagedEvent(type, amount);
        RaiseLocalEvent(limb, ref damaged);

        RefreshBodyDamage(body);
        return true;
    }

    /// <summary>
    /// Heals wounds of the given damage types on a limb.
    /// </summary>
    /// <returns>How much was healed.</returns>
    public FixedPoint2 TryHealLimb(EntityUid limb, IReadOnlyCollection<string> types, FixedPoint2 amount)
    {
        if (_net.IsClient || !_woundableQuery.TryComp(limb, out var woundable))
            return FixedPoint2.Zero;

        var remaining = amount;
        foreach (var wound in GetWoundableWounds(limb, woundable).ToList())
        {
            if (remaining <= 0)
                break;

            if (wound.Comp.IsScar || !types.Contains((string) wound.Comp.DamageType))
                continue;

            var taken = FixedPoint2.Min(wound.Comp.WoundSeverityPoint, remaining);
            if (taken <= 0)
                continue;

            ApplyWoundSeverity(wound, -taken, wound.Comp, woundable);
            remaining -= taken;
        }

        var healed = amount - remaining;
        if (healed <= 0)
            return FixedPoint2.Zero;

        UpdateWoundableIntegrity(limb, woundable);
        CheckWoundableSeverityThresholds(limb, woundable);

        if (TryComp<OrganComponent>(limb, out var organ) && organ.Body is { } body)
            RefreshBodyDamage(body);

        return healed;
    }

    /// <summary>
    /// Makes a limb that was just put back on a body count again. Call <see cref="RefreshBodyDamage"/> once every limb is done.
    /// </summary>
    public void OnLimbReattached(EntityUid organ, EntityUid body)
    {
        if (_net.IsClient || !_woundableQuery.TryComp(organ, out var woundable))
            return;

        // The amputation moved the damage of the limb into the body's own damage, take it back out so it is not counted twice.
        if (TryComp<WoundDamageModelComponent>(body, out var model))
        {
            foreach (var wound in GetWoundableWounds(organ, woundable))
            {
                if (wound.Comp.IsScar)
                    continue;

                var type = (string) wound.Comp.DamageType;
                var left = model.BodyDamage.DamageDict.GetValueOrDefault(type) - wound.Comp.WoundSeverityPoint;
                model.BodyDamage.DamageDict[type] = FixedPoint2.Max(FixedPoint2.Zero, left);
            }
        }

        woundable.WoundableSeverity = WoundableSeverity.Healthy;
        UpdateWoundableIntegrity(organ, woundable);
        CheckWoundableSeverityThresholds(organ, woundable);
        Dirty(organ, woundable);

        // The damage overlay that was hidden when the limb came off shows again.
        if (_visualOrganQuery.TryComp(organ, out var visual) && HasComp<AppearanceComponent>(body))
            _appearance.SetData(body, visual.Layer, false);
    }

    /// <summary>
    /// Adds up the wounds of every limb of the body again, for the damage thresholds and the targeting doll.
    /// </summary>
    public void RefreshBodyDamage(EntityUid body)
    {
        if (_net.IsClient || !TryComp<WoundDamageModelComponent>(body, out var model))
            return;

        SyncBodyDamage((body, model), _body.EnumerateOrgans<WoundableComponent>(body).ToList());
    }
}
