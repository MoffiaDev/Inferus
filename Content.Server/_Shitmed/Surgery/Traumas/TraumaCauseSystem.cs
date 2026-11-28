// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Server/_Shitmed/Medical/Trauma/TraumaCauseSystem.cs) // Inferus
using Content.Shared._Shitmed.Surgery.Traumas;
using Content.Shared._Shitmed.Surgery.Traumas.Systems;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;
using Content.Shared.Explosion;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Shitmed.Surgery.Traumas;

public sealed class TraumaCauseSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private TraumaSystem _trauma = default!;
    [Dependency] private BodySystem _body = default!;

    private static readonly FixedPoint2 CausedTraumaSeverity = 12;

    private readonly List<(TraumaTypePrototype Proto, ExplosionCause Cause)> _explosionCauses = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundDamageModelComponent, BeforeExplodeEvent>(OnBeforeExplode);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        BuildIndex();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<TraumaTypePrototype>())
            BuildIndex();
    }

    private void BuildIndex()
    {
        _explosionCauses.Clear();
        foreach (var proto in _prototype.EnumeratePrototypes<TraumaTypePrototype>())
        {
            if (proto.Causes == null)
                continue;

            foreach (var cause in proto.Causes)
            {
                if (cause is ExplosionCause explosion)
                    _explosionCauses.Add((proto, explosion));
            }
        }
    }

    private void OnBeforeExplode(Entity<WoundDamageModelComponent> ent, ref BeforeExplodeEvent args)
    {
        if (_explosionCauses.Count == 0)
            return;

        var total = args.Damage.GetTotal();
        foreach (var (proto, cause) in _explosionCauses)
        {
            if (total < cause.MinDamage)
                continue;

            var chance = cause.Chance;
            if (cause.ScaleWithDamage && cause.MinDamage > 0)
                chance *= (float) (total / cause.MinDamage);

            if (!_random.Prob(Math.Clamp(chance, 0f, 1f)))
                continue;

            if (TryPickLimb(ent, cause, out var limb))
                _trauma.TryInflictTrauma(limb, new ProtoId<TraumaTypePrototype>(proto.ID), CausedTraumaSeverity);
        }
    }

    /// <summary>
    /// Picks a random attached limb the cause is allowed to hit.
    /// </summary>
    private bool TryPickLimb(EntityUid body, TraumaCause cause, out Entity<WoundableComponent> limb)
    {
        limb = default;
        var candidates = new List<Entity<WoundableComponent>>();

        foreach (var woundable in _body.EnumerateOrgans<WoundableComponent>(body))
        {
            if (cause.PartAllowed(woundable.Comp1.Category))
                candidates.Add((woundable.Owner, woundable.Comp2));
        }

        if (candidates.Count == 0)
            return false;

        limb = _random.Pick(candidates);
        return true;
    }
}