// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Traumas/Systems/TraumaSystem.Bones.cs) // Inferus
// ayyo the pizza here
using System.Linq;
using Content.Shared._Shitmed.Surgery.Wounds;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Shitmed.Surgery.Traumas.Systems;

public sealed partial class BoneSystem : EntitySystem
{
    private static readonly ProtoId<OrganCategoryPrototype> LegLeft = "LegLeft";
    private static readonly ProtoId<OrganCategoryPrototype> LegRight = "LegRight";
    private static readonly ProtoId<OrganCategoryPrototype> FootLeft = "FootLeft";
    private static readonly ProtoId<OrganCategoryPrototype> FootRight = "FootRight";

    private static readonly TimeSpan HealInterval = TimeSpan.FromSeconds(1);

    [Dependency] private INetManager _net = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;

    private TimeSpan _nextHeal;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient || _timing.CurTime < _nextHeal)
            return;

        _nextHeal = _timing.CurTime + HealInterval;

        var query = EntityQueryEnumerator<BoneComponent>();
        while (query.MoveNext(out var uid, out var bone))
        {
            if (bone.BoneIntegrity >= bone.IntegrityCap || bone.BoneSeverity > BoneSeverity.Damaged)
                continue;

            SetIntegrity((uid, bone), bone.BoneIntegrity + bone.NaturalHealing);
        }
    }

    /// <summary>
    /// Every limb that can be wounded has a bone.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnWoundableStartup(Entity<WoundableComponent> ent, ref ComponentStartup args)
    {
        if (_net.IsClient)
            return;

        EnsureComp<BoneComponent>(ent);
    }

    [SubscribeLocalEvent]
    public void RestoreBones(EntityUid body)
    {
        if (_net.IsClient)
            return;

        foreach (var limb in _body.EnumerateOrgans<BoneComponent>(body).ToList())
        {
            SetIntegrity((limb.Owner, limb.Comp2), limb.Comp2.IntegrityCap);
        }
    }

    /// <summary>
    /// Legs and feet slow a body down by the state of their bones. A missing leg is not counted here.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRefreshSpeed(Entity<WoundDamageModelComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        var severities = new Dictionary<ProtoId<OrganCategoryPrototype>, BoneSeverity>();
        foreach (var limb in _body.EnumerateOrgans<BoneComponent>(ent.Owner))
        {
            if (limb.Comp1.Category is { } category)
                severities[category] = limb.Comp2.BoneSeverity;
        }

        var total = 0f;
        var count = 0;
        AddLeg(severities, LegLeft, FootLeft, ref total, ref count);
        AddLeg(severities, LegRight, FootRight, ref total, ref count);

        if (count == 0)
            return;

        var multiplier = total / count;
        if (multiplier < 1f)
            args.ModifySpeed(multiplier);
    }

    private static void AddLeg(
        Dictionary<ProtoId<OrganCategoryPrototype>, BoneSeverity> severities,
        ProtoId<OrganCategoryPrototype> leg,
        ProtoId<OrganCategoryPrototype> foot,
        ref float total,
        ref int count)
    {
        if (!severities.TryGetValue(leg, out var legSeverity))
            return;

        var factor = legSeverity switch
        {
            BoneSeverity.Damaged => 0.9f,
            BoneSeverity.Cracked => 0.7f,
            BoneSeverity.Broken => 0.5f,
            _ => 1f,
        };

        if (severities.TryGetValue(foot, out var footSeverity))
        {
            factor *= footSeverity switch
            {
                BoneSeverity.Damaged => 0.95f,
                BoneSeverity.Cracked => 0.85f,
                BoneSeverity.Broken => 0.75f,
                _ => 1f,
            };
        }

        total += factor;
        count++;
    }

    public bool ApplyBoneDamage(Entity<BoneComponent> bone, FixedPoint2 amount)
    {
        return amount > 0 && SetIntegrity(bone, bone.Comp.BoneIntegrity - amount);
    }

    public bool SetIntegrity(Entity<BoneComponent> bone, FixedPoint2 integrity)
    {
        var comp = bone.Comp;
        var newIntegrity = FixedPoint2.Clamp(integrity, 0, comp.IntegrityCap);
        if (newIntegrity == comp.BoneIntegrity)
            return false;

        var oldIntegrity = comp.BoneIntegrity;
        comp.BoneIntegrity = newIntegrity;

        var oldSeverity = comp.BoneSeverity;
        var newSeverity = GetSeverity(comp);
        comp.BoneSeverity = newSeverity;
        Dirty(bone);

        var changed = new BoneIntegrityChangedEvent(bone, oldIntegrity, newIntegrity);
        RaiseLocalEvent(bone.Owner, ref changed);

        if (newSeverity == oldSeverity)
            return true;

        if (TryComp<OrganComponent>(bone, out var organ) && organ.Body is { } body)
        {
            _movementSpeed.RefreshMovementSpeedModifiers(body);

            if (_net.IsServer && newSeverity > oldSeverity)
            {
                var message = Loc.GetString($"shitmed-bone-damage-{newSeverity.ToString().ToLowerInvariant()}", ("part", Name(bone)));
                _popup.PopupEntity(message, body, body, PopupType.SmallCaution);
            }
        }

        return true;
    }

    private static BoneSeverity GetSeverity(BoneComponent comp)
    {
        foreach (var (severity, value) in comp.Thresholds.OrderByDescending(pair => pair.Value))
        {
            if (comp.BoneIntegrity >= value)
                return severity;
        }

        return BoneSeverity.Broken;
    }
}
