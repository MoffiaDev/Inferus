// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - what a body without a working liver, kidneys or tongue goes through. "Without" covers both a removed organ and a failed one,
// both put the category into LostOrgansComponent.
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Speech;
using Robust.Shared.Timing;

namespace Content.Server._Shitmed.Surgery.Organs;

public sealed class OrganFailureSystem : EntitySystem
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The toxin damage per interval of a body without a liver. Kidneys do half of it, and both stack.
    /// </summary>
    private const float LiverToxin = 1f;
    private const float KidneyToxin = 0.5f;

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    private TimeSpan _next;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LostOrgansComponent, SpeakAttemptEvent>(OnSpeakAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _next)
            return;

        _next = _timing.CurTime + Interval;

        var query = EntityQueryEnumerator<LostOrgansComponent>();
        while (query.MoveNext(out var uid, out var lost))
        {
            var toxin = 0f;
            if (lost.Categories.Contains("Liver"))
                toxin += LiverToxin;

            if (lost.Categories.Contains("Kidneys"))
                toxin += KidneyToxin;

            if (toxin <= 0f || _mobState.IsDead(uid))
                continue;

            var damage = new DamageSpecifier();
            damage.DamageDict["Poison"] = toxin;
            _damageable.TryChangeDamage(uid, damage, ignoreResistances: true, interruptsDoAfters: false);
        }
    }

    private void OnSpeakAttempt(EntityUid uid, LostOrgansComponent comp, SpeakAttemptEvent args)
    {
        if (!comp.Categories.Contains("Tongue"))
            return;

        _popup.PopupEntity(Loc.GetString("shitmed-organ-no-tongue"), uid, uid);
        args.Cancel();
    }
}
