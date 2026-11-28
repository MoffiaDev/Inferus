// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Traumas/TraumaChance.cs) // Inferus
// Inferus - limbs are organs with the bone as a component, and the organs of a limb are its children. There is no nerve component yet,
// so the nerve chance only looks at how hurt the limb is.
using Content.Shared._Shitmed.Surgery.Organs;
using Content.Shared._Shitmed.Surgery.Wounds;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;
using Content.Shared.FixedPoint;

namespace Content.Shared._Shitmed.Surgery.Traumas;

public readonly struct TraumaChanceArgs
{
    public readonly IEntityManager EntityManager;
    public readonly Entity<WoundableComponent> Target;
    public readonly Entity<TraumaInflicterComponent> Inflicter;
    public readonly FixedPoint2 Severity;
    public readonly EntityUid Body;

    public TraumaChanceArgs(
        IEntityManager entityManager,
        Entity<WoundableComponent> target,
        Entity<TraumaInflicterComponent> inflicter,
        FixedPoint2 severity,
        EntityUid body)
    {
        EntityManager = entityManager;
        Target = target;
        Inflicter = inflicter;
        Severity = severity;
        Body = body;
    }
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class TraumaChance
{
    public abstract FixedPoint2? Calculate(in TraumaChanceArgs args);
}

public sealed partial class FlatTraumaChance : TraumaChance
{
    [DataField]
    public FixedPoint2 Chance;

    public override FixedPoint2? Calculate(in TraumaChanceArgs args)
    {
        return Chance;
    }
}

/// <summary>
/// Bone fracture chance, scales with how damaged the limb and its bone already are.
/// </summary>
public sealed partial class BoneFractureChance : TraumaChance
{
    [DataField]
    public Dictionary<WoundableSeverity, FixedPoint2> SeverityMultipliers = new()
    {
        { WoundableSeverity.Healthy, 0 },
        { WoundableSeverity.Minor, 0.01 },
        { WoundableSeverity.Moderate, 0.04 },
        { WoundableSeverity.Severe, 0.12 },
        { WoundableSeverity.Critical, 0.21 },
        { WoundableSeverity.Mangled, 0.21 },
        { WoundableSeverity.Severed, 0 },
    };

    public override FixedPoint2? Calculate(in TraumaChanceArgs args)
    {
        var target = args.Target;
        if (!args.EntityManager.TryGetComponent(target, out BoneComponent? bone)
            || bone.BoneSeverity == BoneSeverity.Broken)
            return null;

        var total = target.Comp.WoundableIntegrity + bone.BoneIntegrity;
        if (total <= 0)
            return null;

        return target.Comp.IntegrityCap / total * SeverityMultipliers.GetValueOrDefault(target.Comp.WoundableSeverity);
    }
}

/// <summary>
/// Nerve damage chance, higher the more hurt the limb is.
/// </summary>
public sealed partial class NerveTraumaChance : TraumaChance
{
    [DataField]
    public FixedPoint2 Divisor = 20;

    public override FixedPoint2? Calculate(in TraumaChanceArgs args)
    {
        var target = args.Target;
        if (target.Comp.IntegrityCap <= 0 || Divisor <= 0)
            return null;

        return target.Comp.WoundableIntegrity / target.Comp.IntegrityCap / Divisor;
    }
}

/// <summary>
/// Organ damage chance, dealt to a random surviving organ in the limb.
/// </summary>
public sealed partial class OrganTraumaChance : TraumaChance
{
    [DataField]
    public FixedPoint2 BaseChance = 0.4;

    public override FixedPoint2? Calculate(in TraumaChanceArgs args)
    {
        var em = args.EntityManager;
        if (!em.TryGetComponent(args.Target, out ParentOrganComponent? parent))
            return null;

        foreach (var child in parent.Children)
        {
            EntityUid uid = child;
            if (em.HasComponent<InternalChildOrganComponent>(uid)
                && em.TryGetComponent(uid, out OrganDamageComponent? damage)
                && damage.Integrity > 0)
                return BaseChance;
        }

        return null;
    }
}

/// <summary>
/// Dismemberment chance, scales with how damaged the limb and its bone already are.
/// </summary>
public sealed partial class DismembermentChance : TraumaChance
{
    [DataField]
    public float IntegrityExponent = 1.3f;

    [DataField]
    public Dictionary<BoneSeverity, float> BoneMultipliers = new()
    {
        { BoneSeverity.Normal, 0.3f },
        { BoneSeverity.Damaged, 0.6f },
        { BoneSeverity.Cracked, 1f },
        { BoneSeverity.Broken, 1.2f },
    };

    /// <summary>
    /// Multiplier per damage type of the wound that rolled this, a type that is not listed counts as 1.
    /// </summary>
    [DataField]
    public Dictionary<string, float> DamageTypeMultipliers = new();

    public override FixedPoint2? Calculate(in TraumaChanceArgs args)
    {
        var em = args.EntityManager;
        var target = args.Target;

        if (!em.TryGetComponent(target, out ChildOrganComponent? relation)
            || relation.Parent == null
            || !target.Comp.CanRemove
            || target.Comp.IntegrityCap <= 0)
            return null;

        var bonePenalty = FixedPoint2.New(1);
        if (em.TryGetComponent(target, out BonelessComponent? boneless))
            bonePenalty = boneless.BonePenalty;

        var multiplier = 1f;
        if (em.TryGetComponent(target, out BoneComponent? bone))
            multiplier = BoneMultipliers.GetValueOrDefault(bone.BoneSeverity, 1f);

        if (DamageTypeMultipliers.Count > 0
            && em.TryGetComponent(args.Inflicter, out WoundComponent? wound))
            multiplier *= DamageTypeMultipliers.GetValueOrDefault((string) wound.DamageType, 1f);

        var cap = target.Comp.IntegrityCap.Float();
        var chance = (1f - (MathF.Pow(target.Comp.WoundableIntegrity.Float(), IntegrityExponent) / cap - 1f) * bonePenalty.Float()) * multiplier;
        return FixedPoint2.New(Math.Max(0f, chance));
    }
}