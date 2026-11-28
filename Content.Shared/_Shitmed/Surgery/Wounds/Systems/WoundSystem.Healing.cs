// SPDX-License-Identifier: AGPL-3.0-or-later
using System.Linq;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Timing;

namespace Content.Shared._Shitmed.Surgery.Wounds.Systems;

public sealed partial class WoundSystem
{
    private static readonly TimeSpan HealInterval = TimeSpan.FromSeconds(1);
    private static readonly FixedPoint2 MinimumHeal = 0.01;

    [Dependency] private IGameTiming _gameTiming = default!;

    private TimeSpan _nextHeal;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient || _gameTiming.CurTime < _nextHeal)
            return;

        _nextHeal = _gameTiming.CurTime + HealInterval;

        var query = EntityQueryEnumerator<WoundDamageModelComponent>();
        while (query.MoveNext(out var uid, out var model))
        {
            if (TryComp<MobStateComponent>(uid, out var mobState) && mobState.CurrentState == MobState.Dead)
                continue;

            HealBody((uid, model));
        }
    }

    private void HealBody(Entity<WoundDamageModelComponent> body)
    {
        var woundables = _body.EnumerateOrgans<WoundableComponent>(body.Owner).ToList();
        var healedAny = false;

        foreach (var limb in woundables)
        {
            // A limb that is badly hurt does not heal on its own, it needs treatment.
            if (!limb.Comp2.CanHealDamage)
                continue;

            var healable = new List<Entity<WoundComponent>>();
            foreach (var wound in GetWoundableWounds(limb, limb.Comp2))
            {
                if (!wound.Comp.IsScar && wound.Comp.CanBeHealed && wound.Comp.WoundSeverityPoint > 0 && !IsHealBlocked(limb, wound))
                    healable.Add(wound);
            }

            if (healable.Count == 0)
                continue;

            // The limb's heal ability is shared between its wounds.
            var share = FixedPoint2.Max(MinimumHeal, limb.Comp2.HealAbility / healable.Count);
            foreach (var wound in healable)
            {
                var amount = FixedPoint2.Min(share, wound.Comp.WoundSeverityPoint);
                ApplyWoundSeverity(wound, -amount, wound.Comp, limb.Comp2);
            }

            UpdateWoundableIntegrity(limb, limb.Comp2);
            CheckWoundableSeverityThresholds(limb, limb.Comp2);
            healedAny = true;
        }

        if (healedAny)
            SyncBodyDamage(body, woundables);
    }
}
