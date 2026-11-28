// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Goob-Station (Content.Server/_Shitmed/DelayedDeath/DelayedDeathSystem.cs) // Inferus
using Content.Server.Chat.Systems;
using Content.Shared._Shitmed.DelayedDeath;
using Content.Shared.Chat;
using Content.Shared.Medical;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;

namespace Content.Server._Shitmed.DelayedDeath;

public sealed class DelayedDeathSystem : EntitySystem
{
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DelayedDeathComponent, TargetBeforeDefibrillatorZapsEvent>(OnDefibZap);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DelayedDeathComponent, MobStateComponent>();
        while (query.MoveNext(out var ent, out var comp, out var mob))
        {
            if (_mobState.IsDead(ent, mob))
                continue;

            comp.DeathTimer += frameTime;
            if (comp.DeathTimer < comp.DeathTime)
                continue;

            // go crit then dead so the death gasp can happen
            _mobState.ChangeMobState(ent, MobState.Critical, mob);
            _mobState.ChangeMobState(ent, MobState.Dead, mob);

            var ev = new DelayedDeathEvent(ent, PreventRevive: comp.PreventAllRevives);
            RaiseLocalEvent(ent, ref ev);

            if (ev.Cancelled)
            {
                RemCompDeferred<DelayedDeathComponent>(ent);
                continue;
            }

            if (!string.IsNullOrWhiteSpace(comp.DeathMessageId))
                _popup.PopupEntity(Loc.GetString(comp.DeathMessageId), ent, ent, PopupType.LargeCaution);
        }
    }

    private void OnDefibZap(EntityUid uid, DelayedDeathComponent comp, TargetBeforeDefibrillatorZapsEvent args)
    {
        // can't defib someone without a heart or brain
        args.Cancel();

        var failMessage = Loc.GetString(comp.DefibFailMessageId);
        _chat.TrySendInGameICMessage(args.Defib, failMessage, InGameICChatType.Speak, true);
    }
}