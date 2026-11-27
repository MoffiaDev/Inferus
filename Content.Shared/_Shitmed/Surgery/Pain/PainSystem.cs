// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - see PainComponent. Pain only exists on the server's word, clients just see the networked level.
using Content.Shared._Shitmed.Surgery.Wounds;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.Stunnable;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Shitmed.Surgery.Pain;

public sealed partial class PainSystem : EntitySystem
{
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStunSystem _stun = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;

    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient || _timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        var query = EntityQueryEnumerator<PainComponent>();
        while (query.MoveNext(out var uid, out var pain))
        {
            if (pain.Pain > 0)
            {
                pain.Pain = Math.Max(0f, pain.Pain - pain.DecayPerSecond * (float) UpdateInterval.TotalSeconds);
                Dirty(uid, pain);
            }

            UpdateLevel((uid, pain));
        }
    }

    /// <summary>
    /// A hit that turned into wound damage on a limb hurts the body the limb belongs to.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnWoundableDamaged(Entity<WoundableComponent> limb, ref WoundableDamagedEvent args)
    {
        if (_net.IsClient
            || !TryComp<OrganComponent>(limb, out var organ)
            || organ.Body is not { } body)
            return;

        var pain = EnsureComp<PainComponent>(body);

        var weight = 1f;
        if (organ.Category is { } category && pain.LimbWeights.TryGetValue(category.Id, out var limbWeight))
            weight = limbWeight;

        AddPain((body, pain), args.Amount.Float() * pain.DamageToPain * weight);
    }

    [SubscribeLocalEvent]
    private void OnRejuvenate(Entity<PainComponent> ent, ref RejuvenateEvent args)
    {
        if (_net.IsClient)
            return;

        ent.Comp.Pain = 0f;
        Dirty(ent);
        UpdateLevel(ent);
    }

    [SubscribeLocalEvent]
    private void OnRefreshSpeed(Entity<PainComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        if (ent.Comp.SpeedMultipliers.TryGetValue(ent.Comp.Level, out var multiplier) && multiplier < 1f)
            args.ModifySpeed(multiplier);
    }

    public void AddPain(Entity<PainComponent> ent, float amount)
    {
        if (_net.IsClient || amount <= 0f)
            return;

        ent.Comp.Pain = Math.Clamp(ent.Comp.Pain + amount, 0f, ent.Comp.MaxPain);
        Dirty(ent);
        UpdateLevel(ent);
    }

    private void UpdateLevel(Entity<PainComponent> ent)
    {
        var comp = ent.Comp;

        var level = PainLevel.None;
        foreach (var (candidate, threshold) in comp.Thresholds)
        {
            if (comp.Pain >= threshold && candidate > level)
                level = candidate;
        }

        var dead = TryComp<MobStateComponent>(ent, out var mobState) && mobState.CurrentState == MobState.Dead;
        if (dead)
            return;

        if (level != comp.Level)
        {
            var old = comp.Level;
            comp.Level = level;
            Dirty(ent);
            _movementSpeed.RefreshMovementSpeedModifiers(ent.Owner);

            if (level > old)
            {
                _popup.PopupEntity(Loc.GetString($"shitmed-pain-{level.ToString().ToLowerInvariant()}"), ent, ent, PopupType.SmallCaution);

                if (comp.KnockdownTimes.TryGetValue(level, out var time))
                {
                    _stun.TryKnockdown(ent.Owner, time, drop: true, force: true);

                    if (level == PainLevel.Agony)
                        comp.NextAgonyKnockdown = _timing.CurTime + comp.AgonyRepeat;
                }
            }
        }
        else if (level == PainLevel.Agony
            && _timing.CurTime >= comp.NextAgonyKnockdown
            && comp.KnockdownTimes.TryGetValue(level, out var repeatTime))
        {
            // Someone who stays in agony keeps going down.
            _stun.TryKnockdown(ent.Owner, repeatTime, drop: true, force: true);
            comp.NextAgonyKnockdown = _timing.CurTime + comp.AgonyRepeat;
        }
    }
}
