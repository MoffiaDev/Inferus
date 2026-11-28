// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Traumas/Systems/TraumaSystem*.cs) // Inferus
// Inferus - traumas hang on wounds like in Goob, but the bone is a component of the limb and the organs are children of the limb.
// BoneDamage hurts the bone of the limb, OrganDamage hurts an internal organ of it, Dismemberment tears the limb off on the next tick.
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Shitmed.Surgery.Organs;
using Content.Shared._Shitmed.Surgery.Wounds;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Surgery.Wounds.Systems;
using Content.Shared.Armor;
using Content.Shared.Body;
using Content.Shared.Inventory;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Shitmed.Surgery.Traumas.Systems;

public sealed partial class TraumaSystem : EntitySystem
{
    public static readonly ProtoId<TraumaTypePrototype> BoneDamage = "BoneDamage";
    public static readonly ProtoId<TraumaTypePrototype> OrganDamage = "OrganDamage";
    public static readonly ProtoId<TraumaTypePrototype> NerveDamage = "NerveDamage";
    public static readonly ProtoId<TraumaTypePrototype> Dismemberment = "Dismemberment";
    public static readonly ProtoId<TraumaTypePrototype> VeinsDamage = "VeinsDamage";
    public static readonly ProtoId<TraumaTypePrototype> Braindeath = "Braindeath";

    private const string TraumaContainerId = "Traumas";

    /// <summary>
    /// The wound that hosts a trauma that has no wound of its own, like the ones from an explosion.
    /// </summary>
    private const string HostWoundType = "Blunt";

    private const float NerveShockFactor = 0.3f;
    private static readonly FixedPoint2 NerveShockCap = 8;

    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private WoundSystem _wound = default!;
    [Dependency] private BoneSystem _bone = default!;
    [Dependency] private OrganDamageSystem _organDamage = default!;
    [Dependency] private InventorySystem _inventory = default!;

    private readonly record struct CauseEntry(ProtoId<TraumaTypePrototype> Id, TraumaTypePrototype Proto, WoundSeverityCause Cause);

    private readonly Dictionary<string, List<CauseEntry>> _woundCauses = new();
    private readonly List<CauseEntry> _anyWoundCauses = new();
    private readonly HashSet<EntityUid> _pendingAmputations = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TraumaInflicterComponent, ComponentInit>(OnInflicterInit);
        SubscribeLocalEvent<TraumaInflicterComponent, EntityTerminatingEvent>(OnInflicterTerminating);
        SubscribeLocalEvent<TraumaInflicterComponent, WoundSeverityPointChangedEvent>(OnWoundSeverityPointChanged);
        SubscribeLocalEvent<TraumaInflicterComponent, WoundHealAttemptEvent>(OnWoundHealAttempt);
        SubscribeLocalEvent<BoneComponent, BoneIntegrityChangedEvent>(OnBoneIntegrityChanged);
        SubscribeLocalEvent<OrganDamageComponent, OrganIntegrityChangedEvent>(OnOrganIntegrityChanged);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
        SubscribeLocalEvent<WoundableComponent, GetTraumaShockEvent>(OnGetTraumaShock);

        BuildCauseIndex();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient || _pendingAmputations.Count == 0)
            return;

        // Limbs are torn off here and not in the middle of a wound update, which would double count the damage they held.
        var limbs = _pendingAmputations.ToList();
        _pendingAmputations.Clear();
        foreach (var limb in limbs)
        {
            if (!TerminatingOrDeleted(limb))
                _wound.TryAmputate(limb);
        }
    }

    #region Setup

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<TraumaTypePrototype>())
            BuildCauseIndex();
    }

    private void BuildCauseIndex()
    {
        _woundCauses.Clear();
        _anyWoundCauses.Clear();

        foreach (var proto in _prototype.EnumeratePrototypes<TraumaTypePrototype>())
        {
            if (proto.Causes == null)
                continue;

            foreach (var cause in proto.Causes)
            {
                if (cause is not WoundSeverityCause wound)
                    continue;

                var entry = new CauseEntry(new ProtoId<TraumaTypePrototype>(proto.ID), proto, wound);
                if (wound.DamageTypes == null || wound.DamageTypes.Count == 0)
                {
                    _anyWoundCauses.Add(entry);
                    continue;
                }

                foreach (var damageType in wound.DamageTypes)
                {
                    if (!_woundCauses.TryGetValue(damageType.Id, out var list))
                        _woundCauses[damageType.Id] = list = new List<CauseEntry>();

                    list.Add(entry);
                }
            }
        }
    }

    private void OnInflicterInit(Entity<TraumaInflicterComponent> ent, ref ComponentInit args)
    {
        ent.Comp.TraumaContainer = _container.EnsureContainer<Container>(ent, TraumaContainerId);
    }

    /// <summary>
    /// A wound that goes away takes its traumas with it, so they get to undo what they did to the body.
    /// </summary>
    private void OnInflicterTerminating(Entity<TraumaInflicterComponent> ent, ref EntityTerminatingEvent args)
    {
        if (_net.IsClient || ent.Comp.TraumaContainer == null)
            return;

        foreach (var trauma in ent.Comp.TraumaContainer.ContainedEntities.ToList())
        {
            if (!TryComp<TraumaComponent>(trauma, out var comp) || comp.TraumaTarget is not { } target)
                continue;

            var ev = new TraumaBeingRemovedEvent((trauma, comp), target, comp.TraumaSeverity, comp.TraumaType);
            RaiseLocalEvent(trauma, ref ev);
        }
    }

    #endregion

    #region Inflicting

    private void OnWoundSeverityPointChanged(Entity<TraumaInflicterComponent> inflicter, ref WoundSeverityPointChangedEvent args)
    {
        if (_net.IsClient)
            return;

        // Overflow is only set when the wound is capped, it then is the damage that did not fit.
        var delta = args.Overflow ?? args.NewSeverity - args.OldSeverity;
        if (delta <= 0 || delta < inflicter.Comp.SeverityThreshold)
            return;

        var holding = args.Component.HoldingWoundable;
        if (!TryComp<WoundableComponent>(holding, out var woundable))
            return;

        var target = new Entity<WoundableComponent>(holding, woundable);
        var rolled = RollTraumas(target, inflicter, delta);
        foreach (var traumaType in rolled)
            ApplyTrauma(target, inflicter, traumaType, delta);

        ApplyMangledTraumas(target, inflicter, delta);
    }

    /// <summary>
    /// A wound that mangled its limb keeps hurting what is under it, with the severity scaled by the multiplier of each trauma type.
    /// </summary>
    private void ApplyMangledTraumas(
        Entity<WoundableComponent> target,
        Entity<TraumaInflicterComponent> inflicter,
        FixedPoint2 severity)
    {
        if (inflicter.Comp.MangledMultipliers is not { Count: > 0 } multipliers
            || !TryComp<WoundComponent>(inflicter, out var wound)
            || wound.MangleSeverity == null
            || !IsMangled(target))
            return;

        foreach (var (traumaType, multiplier) in multipliers)
        {
            if (multiplier <= 0 || !_prototype.TryIndex(traumaType, out var proto))
                continue;

            if (proto.Target == TraumaTarget.Bone && !HasComp<BoneComponent>(target))
                continue;

            ApplyTrauma(target, inflicter, traumaType, FixedPoint2.New(severity.Float() * multiplier.Float()));
        }
    }

    private bool IsMangled(Entity<WoundableComponent> woundable)
    {
        foreach (var wound in _wound.GetWoundableWounds(woundable, woundable.Comp))
        {
            if (!wound.Comp.IsScar && wound.Comp.MangleSeverity is { } mangle && wound.Comp.WoundSeverity >= mangle)
                return true;
        }

        return false;
    }

    public FixedPoint2 GetArmourChanceDeduction(
        EntityUid body,
        Entity<TraumaInflicterComponent> inflicter,
        ProtoId<TraumaTypePrototype> traumaType,
        ProtoId<OrganCategoryPrototype>? category)
    {
        var deduction = FixedPoint2.Zero;
        foreach (var ent in _inventory.GetHandOrInventoryEntities(body, SlotFlags.WITHOUT_POCKET))
        {
            if (!TryComp<ArmorComponent>(ent, out var armour)
                || !armour.TraumaDeductions.TryGetValue(traumaType, out var armourDeduction))
                continue;

            // Armour only makes a trauma rarer, unless the trauma type is allowed to be made likelier by it.
            if (!inflicter.Comp.AllowArmourDeduction.Contains(traumaType) && armourDeduction >= 0)
                continue;

            if (armour.Coverage.Count == 0 || (category is { } cat && armour.Coverage.Contains(cat)))
                deduction += armourDeduction;
        }

        return deduction;
    }

        public List<ProtoId<TraumaTypePrototype>> RollTraumas(
        Entity<WoundableComponent> target,
        Entity<TraumaInflicterComponent> inflicter,
        FixedPoint2 severity)
    {
        var result = new List<ProtoId<TraumaTypePrototype>>();
        if (!TryComp<WoundComponent>(inflicter, out var wound)
            || !TryComp<OrganComponent>(target, out var organ)
            || organ.Body is not { } body)
            return result;

        foreach (var entry in _anyWoundCauses)
            TryRoll(entry, target, inflicter, severity, organ.Category, body, result);

        if (_woundCauses.TryGetValue((string) wound.DamageType, out var eligible))
        {
            foreach (var entry in eligible)
                TryRoll(entry, target, inflicter, severity, organ.Category, body, result);
        }

        return result;
    }

    private void TryRoll(
        CauseEntry entry,
        Entity<WoundableComponent> target,
        Entity<TraumaInflicterComponent> inflicter,
        FixedPoint2 severity,
        ProtoId<OrganCategoryPrototype>? category,
        EntityUid body,
        List<ProtoId<TraumaTypePrototype>> result)
    {
        if (severity < entry.Proto.SeverityGate || !entry.Cause.PartAllowed(category))
            return;

        var chance = GetChance(entry, target, inflicter, severity, body);
        if (chance > 0 && _random.Prob(FixedPoint2.Clamp(chance, 0, 1).Float()))
            result.Add(entry.Id);
    }

    private FixedPoint2 GetChance(
        CauseEntry entry,
        Entity<WoundableComponent> target,
        Entity<TraumaInflicterComponent> inflicter,
        FixedPoint2 severity,
        EntityUid body)
    {
        if (entry.Cause.Chance is not { } provider)
            return entry.Proto.BaseChance;

        var args = new TraumaChanceArgs(EntityManager, target, inflicter, severity, body);
        if (provider.Calculate(in args) is not { } baseChance)
            return FixedPoint2.Zero;

        var deduction = target.Comp.TraumaDeductions.GetValueOrDefault(entry.Id, FixedPoint2.Zero);
        deduction += GetArmourChanceDeduction(body, inflicter, entry.Id, CompOrNull<OrganComponent>(target)?.Category);
        var ev = new TraumaChanceDeductionEvent(severity, entry.Id, FixedPoint2.Zero);
        RaiseLocalEvent(target, ref ev);
        deduction += ev.ChanceDeduction;

        if (deduction >= 1)
            return FixedPoint2.Zero;

        var bonus = inflicter.Comp.TraumasChances.GetValueOrDefault(entry.Id, FixedPoint2.Zero);
        return FixedPoint2.Clamp(baseChance - deduction + bonus, 0, 1);
    }

    /// <summary>
    /// Picks what the trauma hits, runs the events and applies the effect of the trauma type.
    /// </summary>
    private bool ApplyTrauma(
        Entity<WoundableComponent> woundable,
        Entity<TraumaInflicterComponent> inflicter,
        ProtoId<TraumaTypePrototype> traumaType,
        FixedPoint2 severity)
    {
        if (!_prototype.TryIndex(traumaType, out var proto)
            || !TryComp<OrganComponent>(woundable, out var organ)
            || organ.Body is not { } body
            || AtBodyLimit(body, proto))
            return false;

        if (SelectTarget(woundable, proto.Target) is not { } chosen)
            return false;

        var before = new BeforeTraumaInducedEvent(severity, chosen, traumaType);
        RaiseLocalEvent(woundable, ref before);
        if (before.Cancelled)
            return false;

        var apply = new ApplyTraumaEvent(traumaType, woundable, inflicter, chosen, severity);
        RaiseLocalEvent(woundable, ref apply);
        if (apply.Handled)
            return true;

        if (traumaType == Dismemberment)
        {
            // The limb that was hit goes, the trauma would only live on the limb that is torn off.
            _pendingAmputations.Add(woundable);
            return true;
        }

        AddTrauma(chosen, woundable, inflicter, traumaType, severity);

        if (traumaType == BoneDamage && TryComp<BoneComponent>(chosen, out var bone))
            _bone.ApplyBoneDamage((chosen, bone), severity);
        else if (traumaType == OrganDamage && TryComp<OrganDamageComponent>(chosen, out var damage))
            _organDamage.ApplyDamage((chosen, damage), severity);

        return true;
    }

    private EntityUid? SelectTarget(Entity<WoundableComponent> woundable, TraumaTarget target)
    {
        if (target == TraumaTarget.Woundable)
            return woundable;

        if (target == TraumaTarget.Bone)
            return HasComp<BoneComponent>(woundable) ? woundable.Owner : null;

        if (target == TraumaTarget.ParentWoundable)
            return GetParentLimb(woundable);

        if (!TryComp<ParentOrganComponent>(woundable, out var parent))
            return null;

        var candidates = new List<EntityUid>();
        foreach (var child in parent.Children)
        {
            EntityUid uid = child;
            if (HasComp<InternalChildOrganComponent>(uid)
                && TryComp<OrganDamageComponent>(uid, out var damage)
                && damage.Integrity > 0)
                candidates.Add(uid);
        }

        return candidates.Count == 0 ? null : _random.Pick(candidates);
    }

    /// <summary>
    /// The limb a limb is attached to, null for the root of the body.
    /// </summary>
    public EntityUid? GetParentLimb(EntityUid limb)
    {
        return TryComp<ChildOrganComponent>(limb, out var relation)
            && relation.Parent is { } parent
            && HasComp<WoundableComponent>(parent)
                ? parent
                : null;
    }

        private bool AtBodyLimit(EntityUid body, TraumaTypePrototype proto)
    {
        return proto.MaxPerBody is { } max
            && TryGetBodyTraumas(body, out var traumas, new ProtoId<TraumaTypePrototype>(proto.ID))
            && traumas.Count >= max;
    }

    #endregion

    #region Adding and removing

    public EntityUid AddTrauma(
        EntityUid target,
        Entity<WoundableComponent> holding,
        Entity<TraumaInflicterComponent> inflicter,
        ProtoId<TraumaTypePrototype> traumaType,
        FixedPoint2 severity,
        ProtoId<OrganCategoryPrototype>? targetCategory = null)
    {
        if (_net.IsClient || TerminatingOrDeleted(inflicter) || inflicter.Comp.TraumaContainer == null)
            return EntityUid.Invalid;

        foreach (var trauma in inflicter.Comp.TraumaContainer.ContainedEntities)
        {
            if (!TryComp<TraumaComponent>(trauma, out var existing)
                || existing.TraumaType != traumaType
                || existing.TraumaTarget != target)
                continue;

            existing.TraumaSeverity = severity;
            Dirty(trauma, existing);
            return trauma;
        }

        var entityProto = inflicter.Comp.TraumaPrototypes.TryGetValue(traumaType, out var overrideProto)
            ? overrideProto
            : _prototype.Index(traumaType).TraumaEntity;

        var traumaEnt = Spawn(entityProto, MapCoordinates.Nullspace);
        var traumaComp = EnsureComp<TraumaComponent>(traumaEnt);
        traumaComp.TraumaType = traumaType;
        traumaComp.TraumaSeverity = severity;
        traumaComp.TraumaTarget = target;
        traumaComp.TargetCategory = targetCategory;
        traumaComp.HoldingWoundable = holding;

        _container.Insert(traumaEnt, inflicter.Comp.TraumaContainer);

        var onTrauma = new TraumaInducedEvent((traumaEnt, traumaComp), target, severity, traumaType);
        RaiseLocalEvent(traumaEnt, ref onTrauma);

        var onWoundable = new TraumaInducedEvent((traumaEnt, traumaComp), target, severity, traumaType);
        RaiseLocalEvent(holding, ref onWoundable);

        var onInflicter = new TraumaInducedEvent((traumaEnt, traumaComp), target, severity, traumaType);
        RaiseLocalEvent(inflicter, ref onInflicter);

        Dirty(traumaEnt, traumaComp);
        return traumaEnt;
    }

    public void RemoveTrauma(Entity<TraumaComponent> trauma)
    {
        if (!_container.TryGetContainingContainer((trauma.Owner, Transform(trauma.Owner), MetaData(trauma.Owner)), out var container)
            || !TryComp<TraumaInflicterComponent>(container.Owner, out var inflicter))
            return;

        RemoveTrauma(trauma, (container.Owner, inflicter));
    }

    public void RemoveTrauma(Entity<TraumaComponent> trauma, Entity<TraumaInflicterComponent> inflicter)
    {
        if (inflicter.Comp.TraumaContainer != null)
            _container.Remove(trauma.Owner, inflicter.Comp.TraumaContainer, reparent: false, force: true);

        if (trauma.Comp.TraumaTarget is { } target)
        {
            var onTrauma = new TraumaBeingRemovedEvent(trauma, target, trauma.Comp.TraumaSeverity, trauma.Comp.TraumaType);
            RaiseLocalEvent(trauma.Owner, ref onTrauma);

            var onInflicter = new TraumaBeingRemovedEvent(trauma, target, trauma.Comp.TraumaSeverity, trauma.Comp.TraumaType);
            RaiseLocalEvent(inflicter.Owner, ref onInflicter);

            if (trauma.Comp.HoldingWoundable is { } holding && !TerminatingOrDeleted(holding))
            {
                var onWoundable = new TraumaBeingRemovedEvent(trauma, target, trauma.Comp.TraumaSeverity, trauma.Comp.TraumaType);
                RaiseLocalEvent(holding, ref onWoundable);
            }
        }

        if (_net.IsServer)
            QueueDel(trauma);
    }

    /// <summary>
    /// Inflicts a trauma that does not come from a wound, like the ones from an explosion, by hosting it on a fresh blunt wound.
    /// </summary>
    public bool TryInflictTrauma(Entity<WoundableComponent> woundable, ProtoId<TraumaTypePrototype> traumaType, FixedPoint2 severity)
    {
        if (_net.IsClient
            || !TryComp<OrganComponent>(woundable, out var organ)
            || organ.Body is not { } body
            || !_prototype.TryIndex(traumaType, out var proto)
            || AtBodyLimit(body, proto)
            || !_wound.TryInduceWound(woundable, HostWoundType, FixedPoint2.Zero, out var host, woundable.Comp))
            return false;

        var inflicter = EnsureComp<TraumaInflicterComponent>(host.Value.Owner);
        return ApplyTrauma(woundable, (host.Value.Owner, inflicter), traumaType, severity);
    }

    /// <summary>
    /// Inflicts a trauma that is aimed at an organ of the limb, like the one of an organ that was destroyed.
    /// </summary>
    public bool TryInflictOrganTrauma(Entity<WoundableComponent> woundable, EntityUid organ, ProtoId<TraumaTypePrototype> traumaType, FixedPoint2 severity)
    {
        if (_net.IsClient
            || !_wound.TryInduceWound(woundable, HostWoundType, FixedPoint2.Zero, out var host, woundable.Comp))
            return false;

        var inflicter = EnsureComp<TraumaInflicterComponent>(host.Value.Owner);
        AddTrauma(organ, woundable, (host.Value.Owner, inflicter), traumaType, severity);
        return true;
    }

    #endregion

    #region Healing and clearing

    public bool TraumaBlocksHealing(ProtoId<TraumaTypePrototype> traumaType)
    {
        return _prototype.TryIndex(traumaType, out var proto) && proto.BlocksHealing;
    }

    /// <summary>
    /// A trauma only holds a wound back while the part it hurt is still impaired.
    /// </summary>
    private bool StillBlocking(Entity<TraumaComponent> trauma)
    {
        var target = trauma.Comp.TraumaTarget;
        if (trauma.Comp.TraumaType == BoneDamage)
            return TryComp<BoneComponent>(target, out var bone) && bone.BoneSeverity >= BoneSeverity.Cracked;

        if (trauma.Comp.TraumaType == OrganDamage)
            return TryComp<OrganDamageComponent>(target, out var organ) && organ.Severity >= OrganSeverity.Damaged;

        return true;
    }

    private void OnWoundHealAttempt(Entity<TraumaInflicterComponent> inflicter, ref WoundHealAttemptEvent args)
    {
        if (args.IgnoreBlockers)
            return;

        foreach (var trauma in GetWoundTraumas(inflicter.Owner, inflicter.Comp))
        {
            if (!TraumaBlocksHealing(trauma.Comp.TraumaType) || !StillBlocking(trauma))
                continue;

            args.Cancelled = true;
            return;
        }
    }

    private void OnGetTraumaShock(Entity<WoundableComponent> limb, ref GetTraumaShockEvent args)
    {
        var shock = FixedPoint2.Zero;
        foreach (var trauma in GetWoundableTraumas(limb.Owner, NerveDamage))
            shock += FixedPoint2.New(trauma.Comp.TraumaSeverity.Float() * NerveShockFactor);

        args.Shock += FixedPoint2.Min(shock, NerveShockCap);
    }

        private void OnBoneIntegrityChanged(Entity<BoneComponent> bone, ref BoneIntegrityChangedEvent args)
    {
        if (_net.IsClient || args.NewIntegrity < bone.Comp.IntegrityCap)
            return;

        foreach (var trauma in GetWoundableTraumas(bone.Owner, BoneDamage))
        {
            if (trauma.Comp.TraumaTarget == bone.Owner)
                RemoveTrauma(trauma);
        }
    }

    private void OnOrganIntegrityChanged(Entity<OrganDamageComponent> organ, ref OrganIntegrityChangedEvent args)
    {
        if (_net.IsClient || !TryComp<OrganComponent>(organ, out var organComp) || organComp.Body is not { } body)
            return;

        if (args.NewIntegrity >= organ.Comp.IntegrityCap)
        {
            if (TryGetBodyTraumas(body, out var traumas))
            {
                foreach (var trauma in traumas.ToList())
                {
                    if (trauma.Comp.TraumaTarget == organ.Owner)
                        RemoveTrauma(trauma);
                }
            }

            return;
        }

        // An organ that failed can leave a trauma of its own on the limb it sits in, like a dead brain does.
        if (args.NewIntegrity > 0
            || !TryComp<OrganDeathTraumaComponent>(organ, out var death)
            || !TryComp<ChildOrganComponent>(organ, out var relation)
            || relation.Parent is not { } limb
            || !TryComp<WoundableComponent>(limb, out var woundable)
            || HasOrganTrauma(limb, organ.Owner, death.Trauma))
            return;

        TryInflictOrganTrauma((limb, woundable), organ.Owner, death.Trauma, organ.Comp.IntegrityCap);
    }

    private bool HasOrganTrauma(EntityUid woundable, EntityUid organ, ProtoId<TraumaTypePrototype> traumaType)
    {
        foreach (var trauma in GetWoundableTraumas(woundable, traumaType))
        {
            if (trauma.Comp.TraumaTarget == organ)
                return true;
        }

        return false;
    }

    #endregion

    #region Queries

    public IEnumerable<Entity<TraumaComponent>> GetWoundTraumas(EntityUid wound, TraumaInflicterComponent? inflicter = null)
    {
        if (!Resolve(wound, ref inflicter, false) || inflicter.TraumaContainer == null)
            yield break;

        foreach (var trauma in inflicter.TraumaContainer.ContainedEntities)
        {
            if (TryComp<TraumaComponent>(trauma, out var comp))
                yield return (trauma, comp);
        }
    }

    public List<Entity<TraumaComponent>> GetWoundableTraumas(EntityUid woundable, ProtoId<TraumaTypePrototype>? traumaType = null)
    {
        var result = new List<Entity<TraumaComponent>>();
        if (!TryComp<WoundableComponent>(woundable, out var woundableComp))
            return result;

        foreach (var wound in _wound.GetWoundableWounds(woundable, woundableComp))
        {
            if (!TryComp<TraumaInflicterComponent>(wound.Owner, out var inflicter))
                continue;

            foreach (var trauma in GetWoundTraumas(wound.Owner, inflicter))
            {
                if (traumaType != null && trauma.Comp.TraumaType != traumaType.Value)
                    continue;

                result.Add(trauma);
            }
        }

        return result;
    }

    public bool TryGetWoundableTraumas(
        EntityUid woundable,
        [NotNullWhen(true)] out List<Entity<TraumaComponent>>? traumas,
        ProtoId<TraumaTypePrototype>? traumaType = null)
    {
        traumas = GetWoundableTraumas(woundable, traumaType);
        return traumas.Count > 0;
    }

    public bool TryGetBodyTraumas(
        EntityUid body,
        [NotNullWhen(true)] out List<Entity<TraumaComponent>>? traumas,
        ProtoId<TraumaTypePrototype>? traumaType = null)
    {
        traumas = new List<Entity<TraumaComponent>>();
        foreach (var limb in _body.EnumerateOrgans<WoundableComponent>(body))
            traumas.AddRange(GetWoundableTraumas(limb.Owner, traumaType));

        return traumas.Count > 0;
    }

    public bool HasBodyTrauma(EntityUid body, ProtoId<TraumaTypePrototype>? traumaType = null)
    {
        return TryGetBodyTraumas(body, out _, traumaType);
    }

    #endregion
}