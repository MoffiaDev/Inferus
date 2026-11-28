// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Server/_Shitmed/Medical/Trauma/TraumaBloodlossSystem.cs) // Inferus
// Inferus - the floor is added up from the traumas every second instead of being counted up and down by events. A limb that is torn off,
// or a wound that goes away, then stops adding to the bleed of the body without anything having to undo it.
using Content.Shared._Shitmed.Surgery.Traumas;
using Content.Shared.Body;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Robust.Shared.Timing;

namespace Content.Server._Shitmed.Surgery.Traumas;

public sealed class TraumaBloodlossSystem : EntitySystem
{
    [Dependency] private BloodstreamSystem _blood = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    private readonly Dictionary<EntityUid, float> _floors = new();
    private TimeSpan _nextUpdate;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_timing.CurTime < _nextUpdate)
            return;

        _nextUpdate = _timing.CurTime + UpdateInterval;
        _floors.Clear();

        var query = EntityQueryEnumerator<TraumaBloodlossComponent, TraumaComponent>();
        while (query.MoveNext(out _, out var loss, out var trauma))
        {
            if (trauma.HoldingWoundable is not { } holding
                || !TryComp<OrganComponent>(holding, out var organ)
                || organ.Body is not { } body)
                continue;

            _floors[body] = _floors.GetValueOrDefault(body) + loss.Amount;
        }

        foreach (var (body, floor) in _floors)
        {
            if (!TryComp<BloodstreamComponent>(body, out var bloodstream))
                continue;

            var deficit = floor - bloodstream.BleedAmount;
            if (deficit > 0)
                _blood.TryModifyBleedAmount((body, bloodstream), deficit);
        }
    }
}