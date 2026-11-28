// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Traumas/Systems/TraumaSystem.Organs.cs) // Inferus
// Inferus - hits on the torso and head hurt the organs inside. A failed organ is lost to the body (it counts like a removed one for the
// vital organs, eyes, ears and lungs) until surgery repairs it.
using System.Linq;
using Content.Shared._Shitmed.Surgery.Wounds;
using Content.Shared._Shitmed.Surgery.Wounds.Systems;
using Content.Shared.Body;
using Content.Shared.Emp; // Inferus
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Shitmed.Surgery.Organs;

public sealed partial class OrganDamageSystem : EntitySystem
{
    private static readonly TimeSpan HealInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Hits that deal less wound damage than this never hurt an organ.
    /// </summary>
    private static readonly FixedPoint2 MinimumDamage = 6;

    /// <summary>
    /// A hit this strong always rolls a success, weaker hits scale down to the minimum.
    /// </summary>
    private const float FullChanceDamage = 30f;

    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private WoundSystem _wounds = default!;

    private TimeSpan _nextHeal;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient || _timing.CurTime < _nextHeal)
            return;

        _nextHeal = _timing.CurTime + HealInterval;

        var query = EntityQueryEnumerator<OrganDamageComponent>();
        while (query.MoveNext(out var uid, out var organ))
        {
            if (organ.Integrity >= organ.IntegrityCap || organ.Severity > OrganSeverity.Bruised)
                continue;

            SetIntegrity((uid, organ), organ.Integrity + organ.NaturalHealing);
        }
    }

    /// <summary>
    /// Every internal organ can be damaged.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnInternalOrganStartup(Entity<InternalChildOrganComponent> ent, ref ComponentStartup args)
    {
        if (_net.IsClient)
            return;

        EnsureComp<OrganDamageComponent>(ent);
    }

    [SubscribeLocalEvent]
    private void OnWoundableDamaged(Entity<ParentOrganComponent> limb, ref WoundableDamagedEvent args)
    {
        if (_net.IsClient || args.Amount < MinimumDamage)
            return;

        var chance = Math.Min(1f, args.Amount.Float() / FullChanceDamage);
        if (!_random.Prob(chance))
            return;

        var candidates = new List<Entity<OrganDamageComponent>>();
        foreach (var child in limb.Comp.Children)
        {
            EntityUid uid = child;
            if (HasComp<InternalChildOrganComponent>(uid)
                && TryComp<OrganDamageComponent>(uid, out var damage)
                && damage.DamageFactors.ContainsKey(args.DamageType))
                candidates.Add((uid, damage));
        }

        if (candidates.Count == 0)
            return;

        var organ = _random.Pick(candidates);
        var amount = FixedPoint2.New(args.Amount.Float() * organ.Comp.DamageFactors[args.DamageType]);
        ApplyDamage(organ, amount);
    }

    public bool ApplyDamage(Entity<OrganDamageComponent> organ, FixedPoint2 amount)
    {
        return amount > 0 && SetIntegrity(organ, organ.Comp.Integrity - amount);
    }

    public bool Heal(Entity<OrganDamageComponent> organ, FixedPoint2 amount)
    {
        return amount > 0 && SetIntegrity(organ, organ.Comp.Integrity + amount);
    }

    public bool SetIntegrity(Entity<OrganDamageComponent> organ, FixedPoint2 integrity)
    {
        var comp = organ.Comp;
        var newIntegrity = FixedPoint2.Clamp(integrity, 0, comp.IntegrityCap);
        if (newIntegrity == comp.Integrity)
            return false;

        comp.Integrity = newIntegrity;

        var oldSeverity = comp.Severity;
        var newSeverity = GetSeverity(comp);
        comp.Severity = newSeverity;
        Dirty(organ);

        if (newSeverity == oldSeverity)
            return true;

        if (TryComp<OrganComponent>(organ, out var organComp) && organComp.Body is { } body)
        {
            // A failed organ is lost to the body, a repaired one is back.
            if ((oldSeverity == OrganSeverity.Failed || newSeverity == OrganSeverity.Failed) && organComp.Category is { } category)
                _wounds.SetOrganLost(body, category, newSeverity == OrganSeverity.Failed || HasComp<EmpDisabledComponent>(organ)); // Inferus - an EMP keeps a machine organ off

            if (_net.IsServer && newSeverity > oldSeverity)
            {
                var message = Loc.GetString($"shitmed-organ-damage-{newSeverity.ToString().ToLowerInvariant()}", ("organ", Name(organ)));
                _popup.PopupEntity(message, body, body, PopupType.SmallCaution);
            }
        }

        return true;
    }

    private static OrganSeverity GetSeverity(OrganDamageComponent comp)
    {
        foreach (var (severity, value) in comp.Thresholds.OrderByDescending(pair => pair.Value))
        {
            if (comp.Integrity >= value)
                return severity;
        }

        return OrganSeverity.Failed;
    }
}
