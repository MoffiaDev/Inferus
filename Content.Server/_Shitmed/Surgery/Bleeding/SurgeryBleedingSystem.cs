// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - a limb with an open incision bleeds until the vessels are clamped. The bleeding goes into the bloodstream like a wound's does,
// so it fills the bleeding alert, leaks blood and is stopped by a tourniquet. LimbBleedingSystem counts it in the limb's bleeding.
using Content.Shared._Shitmed.Surgery.Operations;
using Content.Shared.Body;
using Content.Shared.Body.Systems;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Shitmed.Surgery.Bleeding;

public sealed partial class SurgeryBleedingSystem : EntitySystem
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How much bleed rate every open limb adds per interval. The bloodstream lets the bleed rate fade by itself, so this keeps it up.
    /// </summary>
    private const float BleedPerInterval = 0.6f;

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private BloodstreamSystem _bloodstream = default!;
    [Dependency] private MobStateSystem _mobState = default!;

    private TimeSpan _next;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _next)
            return;

        _next = _timing.CurTime + Interval;

        var query = EntityQueryEnumerator<IncisionOpenComponent, OrganComponent>();
        while (query.MoveNext(out var uid, out _, out var organ))
        {
            if (organ.Body is not { } body
                || HasComp<BleedersClampedComponent>(uid)
                || _mobState.IsDead(body))
                continue;

            _bloodstream.TryModifyBleedAmount(body, BleedPerInterval);
        }
    }
}
