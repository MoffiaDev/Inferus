// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - see ConsciousnessComponent. Everything here only runs on the server.
using Content.Shared._Shitmed.Surgery.Pain;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Shitmed.Surgery.Consciousness;

public sealed partial class ConsciousnessSystem : EntitySystem
{
    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private static readonly ProtoId<OrganCategoryPrototype> Head = "Head";

    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private StatusEffectsSystem _status = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private TimeSpan _nextUpdate;
    private readonly List<EntityUid> _missing = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient || _timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;

        // Every body with wounds gets the component, the first time it is seen here.
        _missing.Clear();
        var bodies = EntityQueryEnumerator<WoundDamageModelComponent>();
        while (bodies.MoveNext(out var uid, out _))
        {
            if (!HasComp<ConsciousnessComponent>(uid))
                _missing.Add(uid);
        }

        foreach (var uid in _missing)
        {
            EnsureComp<ConsciousnessComponent>(uid);
        }

        var query = EntityQueryEnumerator<ConsciousnessComponent>();
        while (query.MoveNext(out var uid, out var consciousness))
        {
            Refresh((uid, consciousness));
        }
    }

    [SubscribeLocalEvent]
    private void OnRejuvenate(Entity<ConsciousnessComponent> ent, ref RejuvenateEvent args)
    {
        if (_net.IsClient)
            return;

        ent.Comp.Value = 100f;
        ent.Comp.Woozy = false;

        if (ent.Comp.Unconscious)
        {
            ent.Comp.Unconscious = false;
            _status.TryRemoveStatusEffect(ent, SleepingSystem.StatusEffectForcedSleeping);
        }

        Dirty(ent);
    }

    private void Refresh(Entity<ConsciousnessComponent> ent)
    {
        var comp = ent.Comp;

        if (TryComp<MobStateComponent>(ent, out var mobState) && mobState.CurrentState == MobState.Dead)
            return;

        var penalty = 0f;

        if (TryComp<PainComponent>(ent, out var pain) && pain.Pain > comp.PainFree)
            penalty += (pain.Pain - comp.PainFree) * comp.PainFactor;

        if (HasComp<BloodstreamComponent>(ent))
        {
            var blood = _bloodstream.GetBloodLevel(ent.Owner);
            if (blood < comp.BloodStart)
                penalty += Math.Clamp((comp.BloodStart - blood) / (comp.BloodStart - comp.BloodEnd), 0f, 1f) * 100f;
        }

        foreach (var limb in _body.EnumerateOrgans<WoundableComponent>(ent.Owner))
        {
            if (limb.Comp1.Category != Head)
                continue;

            if (comp.HeadPenalties.TryGetValue(limb.Comp2.WoundableSeverity, out var headPenalty))
                penalty += headPenalty;

            break;
        }

        var value = Math.Clamp(100f - penalty, 0f, 100f);
        var changed = Math.Abs(value - comp.Value) >= 0.5f;
        comp.Value = value;

        if (!comp.Unconscious && value <= comp.PassOutAt)
        {
            comp.Unconscious = true;
            comp.Woozy = true;
            _popup.PopupEntity(Loc.GetString("shitmed-consciousness-pass-out"), ent, ent, PopupType.LargeCaution);
            changed = true;
        }
        else if (comp.Unconscious && value >= comp.WakeAt)
        {
            comp.Unconscious = false;
            _popup.PopupEntity(Loc.GetString("shitmed-consciousness-wake"), ent, ent, PopupType.Medium);
            changed = true;
        }
        else if (!comp.Unconscious && !comp.Woozy && value <= comp.WoozyAt)
        {
            comp.Woozy = true;
            _popup.PopupEntity(Loc.GetString("shitmed-consciousness-woozy"), ent, ent, PopupType.SmallCaution);
        }
        else if (comp.Woozy && !comp.Unconscious && value > comp.WoozyAt + 10f)
        {
            comp.Woozy = false;
        }

        if (comp.Unconscious)
            _status.TryAddStatusEffectDuration(ent, SleepingSystem.StatusEffectForcedSleeping, comp.SleepRefresh);

        if (changed)
            Dirty(ent);
    }
}
