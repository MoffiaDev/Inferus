// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Wounds/Systems/WoundSystem*.cs) // Inferus
// Inferus - woundables are organs (see OrganComponent / OrganRelationSystem) instead of BodyPartComponent entities.
// Healing over time, bleeding, destruction (dismemberment), traumas, pain and the visual layer come in later stages.
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Shitmed.Surgery.Wounds.Systems;

public sealed partial class WoundSystem : EntitySystem
{
    private const string WoundContainerId = "Wounds";
    private const string TorsoCategory = "Torso";

    private static readonly KeyValuePair<WoundSeverity, FixedPoint2>[] WoundThresholds =
    [
        new(WoundSeverity.Loss, 100),
        new(WoundSeverity.Critical, 80),
        new(WoundSeverity.Severe, 50),
        new(WoundSeverity.Moderate, 25),
        new(WoundSeverity.Minor, 1),
        new(WoundSeverity.Healed, 0),
    ];

    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private DamageableSystem _damageable = default!;

    [Dependency] private EntityQuery<WoundableComponent> _woundableQuery = default!;
    [Dependency] private EntityQuery<WoundComponent> _woundQuery = default!;
    [Dependency] private EntityQuery<DamageableComponent> _damageableQuery = default!;

    private readonly Dictionary<string, DamageGroupPrototype> _damageTypeToGroup = new();

    // Inferus - whether a damage type has a wound prototype, so the lookup does not run (and log) on every hit
    private readonly Dictionary<string, bool> _validWoundTypes = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundComponent, ComponentGetState>(OnWoundGetState);
        SubscribeLocalEvent<WoundComponent, ComponentHandleState>(OnWoundHandleState);
        SubscribeLocalEvent<WoundableComponent, ComponentGetState>(OnWoundableGetState);
        SubscribeLocalEvent<WoundableComponent, ComponentHandleState>(OnWoundableHandleState);
        SubscribeLocalEvent<WoundableComponent, ComponentInit>(OnWoundableInit);
        SubscribeLocalEvent<WoundComponent, WoundSeverityChangedEvent>(OnWoundSeverityChanged);
        SubscribeLocalEvent<WoundDamageModelComponent, DamageDealtEvent>(OnDamageDealt);

        BuildDamageTypeToGroupCache();
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    #region Setup and networking

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<DamageGroupPrototype>())
            BuildDamageTypeToGroupCache();
        if (args.WasModified<EntityPrototype>())
            _validWoundTypes.Clear();
    }

    private void BuildDamageTypeToGroupCache()
    {
        _damageTypeToGroup.Clear();
        foreach (var group in ProtoMan.EnumeratePrototypes<DamageGroupPrototype>())
        {
            foreach (var damageType in group.DamageTypes)
                _damageTypeToGroup[damageType] = group;
        }
    }

    private void OnWoundableInit(Entity<WoundableComponent> ent, ref ComponentInit args)
    {
        ent.Comp.RootWoundable = ent;
        ent.Comp.Wounds = _container.EnsureContainer<Container>(ent, WoundContainerId);
        ent.Comp.SortedThresholds = ent.Comp.Thresholds.OrderByDescending(kv => kv.Value).ToArray();
    }

    private void OnWoundGetState(Entity<WoundComponent> ent, ref ComponentGetState args)
    {
        var comp = ent.Comp;
        args.State = new WoundComponentState
        {
            HoldingWoundable = TryGetNetEntity(comp.HoldingWoundable, out var holding) ? holding.Value : NetEntity.Invalid,
            WoundSeverityPoint = comp.WoundSeverityPoint,
            WoundType = comp.WoundType,
            DamageGroup = comp.DamageGroup,
            DamageType = comp.DamageType,
            ScarWound = comp.ScarWound,
            IsScar = comp.IsScar,
            WoundSeverity = comp.WoundSeverity,
            WoundVisibility = comp.WoundVisibility,
            CanBeHealed = comp.CanBeHealed,
            SelfHealMultiplier = comp.SelfHealMultiplier,
        };
    }

    private void OnWoundHandleState(Entity<WoundComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not WoundComponentState state)
            return;

        var comp = ent.Comp;
        comp.HoldingWoundable = TryGetEntity(state.HoldingWoundable, out var holding) ? holding.Value : EntityUid.Invalid;
        comp.WoundSeverityPoint = state.WoundSeverityPoint;
        comp.WoundType = state.WoundType;
        comp.DamageGroup = state.DamageGroup;
        if (state.DamageType != null)
            comp.DamageType = state.DamageType;
        comp.ScarWound = state.ScarWound;
        comp.IsScar = state.IsScar;
        comp.WoundSeverity = state.WoundSeverity;
        comp.WoundVisibility = state.WoundVisibility;
        comp.CanBeHealed = state.CanBeHealed;
        comp.SelfHealMultiplier = state.SelfHealMultiplier;
    }

    private void OnWoundableGetState(Entity<WoundableComponent> ent, ref ComponentGetState args)
    {
        var comp = ent.Comp;

        var severityMultipliers = new Dictionary<NetEntity, WoundableSeverityMultiplier>(comp.SeverityMultipliers.Count);
        foreach (var (key, value) in comp.SeverityMultipliers)
            severityMultipliers[TryGetNetEntity(key, out var ne) ? ne.Value : NetEntity.Invalid] = value;

        var healingMultipliers = new Dictionary<NetEntity, WoundableHealingMultiplier>(comp.HealingMultipliers.Count);
        foreach (var (key, value) in comp.HealingMultipliers)
            healingMultipliers[TryGetNetEntity(key, out var ne) ? ne.Value : NetEntity.Invalid] = value;

        args.State = new WoundableComponentState
        {
            AllowWounds = comp.AllowWounds,
            DamageContainerID = comp.DamageContainerID,
            DodgeChance = comp.DodgeChance,
            Bleeds = comp.Bleeds,
            WoundableIntegrity = comp.WoundableIntegrity,
            HealAbility = comp.HealAbility,
            SeverityMultipliers = severityMultipliers,
            HealingMultipliers = healingMultipliers,
            WoundableSeverity = comp.WoundableSeverity,
        };
    }

    private void OnWoundableHandleState(Entity<WoundableComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not WoundableComponentState state)
            return;

        var comp = ent.Comp;
        comp.AllowWounds = state.AllowWounds;
        comp.DamageContainerID = state.DamageContainerID;
        comp.DodgeChance = state.DodgeChance;
        comp.HealAbility = state.HealAbility;
        comp.Bleeds = state.Bleeds;

        comp.SeverityMultipliers.Clear();
        foreach (var (key, value) in state.SeverityMultipliers)
        {
            if (TryGetEntity(key, out var entityKey))
                comp.SeverityMultipliers[entityKey.Value] = value;
        }

        comp.HealingMultipliers.Clear();
        foreach (var (key, value) in state.HealingMultipliers)
        {
            if (TryGetEntity(key, out var entityKey))
                comp.HealingMultipliers[entityKey.Value] = value;
        }

        if (comp.WoundableIntegrity != state.WoundableIntegrity)
        {
            var ev = new WoundableIntegrityChangedEvent(comp.WoundableIntegrity, state.WoundableIntegrity);
            RaiseLocalEvent(ent, ref ev);
        }

        comp.WoundableIntegrity = state.WoundableIntegrity;

        if (comp.WoundableSeverity != state.WoundableSeverity)
        {
            var ev = new WoundableSeverityChangedEvent(comp.WoundableSeverity, state.WoundableSeverity);
            RaiseLocalEvent(ent, ref ev);
        }

        comp.WoundableSeverity = state.WoundableSeverity;
    }

    private void OnWoundSeverityChanged(Entity<WoundComponent> ent, ref WoundSeverityChangedEvent args)
    {
        if (args.NewSeverity != WoundSeverity.Healed)
            return;

        RemoveWound(ent, ent.Comp);
    }

    #endregion

    #region Damage model

    private void OnDamageDealt(Entity<WoundDamageModelComponent> ent, ref DamageDealtEvent args)
    {
        // Inferus - wounds are server authoritative for now, so clients do not predict damage on wound bodies.
        if (_net.IsClient)
            return;

        var woundables = _body.EnumerateOrgans<WoundableComponent>(ent.Owner).ToList();
        var limb = PickWoundable(woundables, args.Origin);

        foreach (var (type, value) in args.Damage.DamageDict)
        {
            if (value > 0)
                ApplyDamage(ent, woundables, limb, type, value);
            else if (value < 0)
                ApplyHealing(ent, woundables, type, -value);
        }

        SyncBodyDamage(ent, woundables, args.Origin, args.InterruptsDoAfters);
    }

    /// <summary>
    /// Picks the limb a hit lands on. Uses the attacker's targeting doll and hit odds when there is one, otherwise a random limb weighted by integrity.
    /// </summary>
    private Entity<OrganComponent, WoundableComponent>? PickWoundable(
        List<Entity<OrganComponent, WoundableComponent>> woundables,
        EntityUid? origin)
    {
        if (woundables.Count == 0)
            return null;

        if (origin is { } attacker && TryComp<TargetingComponent>(attacker, out var targeting))
        {
            var category = SharedTargetingSystem.ToOrganCategory(RollHit(targeting));
            foreach (var woundable in woundables)
            {
                if (woundable.Comp1.Category == category)
                    return woundable;
            }

            // The targeted limb is missing, so the hit lands on the torso.
            return FindByCategory(woundables, TorsoCategory) ?? woundables[0];
        }

        var total = FixedPoint2.Zero;
        foreach (var woundable in woundables)
            total += woundable.Comp2.IntegrityCap;

        if (total <= 0)
            return woundables[0];

        var roll = _random.NextFloat() * total.Float();
        foreach (var woundable in woundables)
        {
            roll -= woundable.Comp2.IntegrityCap.Float();
            if (roll <= 0)
                return woundable;
        }

        return woundables[^1];
    }

    private TargetBodyPart RollHit(TargetingComponent targeting)
    {
        if (!targeting.TargetOdds.TryGetValue(targeting.Target, out var odds) || odds.Count == 0)
            return targeting.Target;

        var roll = _random.NextFloat();
        var accumulated = 0f;
        foreach (var (part, chance) in odds)
        {
            accumulated += chance;
            if (roll < accumulated)
                return part;
        }

        return targeting.Target;
    }

    private static Entity<OrganComponent, WoundableComponent>? FindByCategory(
        List<Entity<OrganComponent, WoundableComponent>> woundables,
        ProtoId<OrganCategoryPrototype> category)
    {
        foreach (var woundable in woundables)
        {
            if (woundable.Comp1.Category == category)
                return woundable;
        }

        return null;
    }

    private void ApplyDamage(
        Entity<WoundDamageModelComponent> body,
        List<Entity<OrganComponent, WoundableComponent>> woundables,
        Entity<OrganComponent, WoundableComponent>? limb,
        string type,
        FixedPoint2 amount)
    {
        var remaining = amount;

        if (IsWoundPrototypeValid(type))
        {
            // The hit limb takes what it can, then the torso, and whatever is left over becomes body damage.
            var candidates = new List<Entity<OrganComponent, WoundableComponent>>(2);
            if (limb is { } hit)
                candidates.Add(hit);
            if (FindByCategory(woundables, TorsoCategory) is { } torso && (limb == null || torso.Owner != limb.Value.Owner))
                candidates.Add(torso);

            foreach (var candidate in candidates)
            {
                if (remaining <= 0)
                    break;

                var capacity = candidate.Comp2.WoundableIntegrity;
                if (capacity <= 0 || !candidate.Comp2.AllowWounds)
                {
                    // Inferus - a limb with no integrity left can be torn off by the hit
                    if (capacity <= 0)
                        TryDismember(body, woundables, candidate, type, remaining);

                    continue;
                }

                var taken = FixedPoint2.Min(remaining, capacity);
                if (TryInduceWound(candidate, type, taken, out _, candidate.Comp2))
                    remaining -= taken;
            }
        }

        if (remaining <= 0)
            return;

        var dict = body.Comp.BodyDamage.DamageDict;
        dict[type] = dict.GetValueOrDefault(type) + remaining;
    }

    private void ApplyHealing(
        Entity<WoundDamageModelComponent> body,
        List<Entity<OrganComponent, WoundableComponent>> woundables,
        string type,
        FixedPoint2 amount)
    {
        var remaining = amount;

        var dict = body.Comp.BodyDamage.DamageDict;
        if (dict.TryGetValue(type, out var bodyDamage) && bodyDamage > 0)
        {
            var taken = FixedPoint2.Min(bodyDamage, remaining);
            dict[type] = bodyDamage - taken;
            remaining -= taken;
        }

        foreach (var limb in woundables)
        {
            if (remaining <= 0)
                break;

            foreach (var wound in GetWoundableWounds(limb, limb.Comp2).ToList())
            {
                if (remaining <= 0)
                    break;

                if (wound.Comp.IsScar || !wound.Comp.CanBeHealed || (string) wound.Comp.DamageType != type)
                    continue;

                var taken = FixedPoint2.Min(wound.Comp.WoundSeverityPoint, remaining);
                if (taken <= 0)
                    continue;

                ApplyWoundSeverity(wound, -taken, wound.Comp, limb.Comp2);
                remaining -= taken;
            }

            UpdateWoundableIntegrity(limb, limb.Comp2);
            CheckWoundableSeverityThresholds(limb, limb.Comp2);
        }
    }

    /// <summary>
    /// Writes the sum of every wound plus the body damage into the body's <see cref="DamageableComponent"/>,
    /// which is what mob thresholds read to decide on crit and death.
    /// </summary>
    private void SyncBodyDamage(
        Entity<WoundDamageModelComponent> body,
        List<Entity<OrganComponent, WoundableComponent>> woundables,
        EntityUid? origin = null,
        bool interruptsDoAfters = true)
    {
        if (!_damageableQuery.TryComp(body, out var damageable))
            return;

        var total = new DamageSpecifier();

    #pragma warning disable CS0618 // GetAllDamage is obsolete, but it is the only public way to read the tracked damage
        var before = _damageable.GetAllDamage((body.Owner, damageable));
    #pragma warning restore CS0618

        // Keep every type the body already tracks.
        foreach (var type in before.DamageDict.Keys)
            total.DamageDict[type] = FixedPoint2.Zero;

        foreach (var (type, value) in body.Comp.BodyDamage.DamageDict)
            total.DamageDict[type] = total.DamageDict.GetValueOrDefault(type) + value;

        foreach (var limb in woundables)
        {
            foreach (var wound in GetWoundableWounds(limb, limb.Comp2))
            {
                if (wound.Comp.IsScar)
                    continue;

                var type = (string) wound.Comp.DamageType;
                total.DamageDict[type] = total.DamageDict.GetValueOrDefault(type) + wound.Comp.WoundSeverityPoint;
            }
        }

        var delta = total - before;
        delta.TrimZeros();

        _damageable.SetDamage((body.Owner, damageable), total, delta, interruptsDoAfters, origin);
        UpdateBodyStatus(body.Owner, woundables);
    }

    #endregion

    #region Wounds

    public DamageGroupPrototype? GetDamageGroupByType(string id)
    {
        return _damageTypeToGroup.GetValueOrDefault(id);
    }

    /// <summary>
    /// A wound prototype is an entity prototype whose ID is the damage type it represents and that has a <see cref="WoundComponent"/>.
    /// </summary>
        private bool IsWoundPrototypeValid(string protoId)
    {
        if (_validWoundTypes.TryGetValue(protoId, out var valid))
            return valid;

        // Inferus - HasIndex does not log, a missing wound prototype is normal for damage types like Structural
        var id = new ProtoId<EntityPrototype>(WoundPrototypeId(protoId));
        valid = ProtoMan.HasIndex(id) && ProtoMan.Index(id).TryGetComponent<WoundComponent>(out _, _factory);
        _validWoundTypes[protoId] = valid;
        return valid;
    }

    private static string WoundPrototypeId(string damageType)
    {
        return $"Wound{damageType}";
    }

    public IEnumerable<Entity<WoundComponent>> GetWoundableWounds(EntityUid woundableUid, WoundableComponent? woundable = null)
    {
        if (!Resolve(woundableUid, ref woundable, false)
            || woundable.Wounds == null
            || woundable.Wounds.Count == 0)
            yield break;

        foreach (var woundEntity in woundable.Wounds.ContainedEntities)
        {
            if (_woundQuery.TryComp(woundEntity, out var wound))
                yield return (woundEntity, wound);
        }
    }

    public bool TryInduceWound(
        EntityUid uid,
        string woundId,
        FixedPoint2 severity,
        [NotNullWhen(true)] out Entity<WoundComponent>? woundInduced,
        WoundableComponent? woundable = null)
    {
        woundInduced = null;
        if (!Resolve(uid, ref woundable))
            return false;

        if (TryContinueWound(uid, woundId, severity, out woundInduced, woundable))
            return true;

        return TryCreateWound(uid, woundId, severity, out woundInduced, GetDamageGroupByType(woundId)?.ID, woundable);
    }

    public bool TryCreateWound(
        EntityUid uid,
        string woundProtoId,
        FixedPoint2 severity,
        [NotNullWhen(true)] out Entity<WoundComponent>? woundCreated,
        ProtoId<DamageGroupPrototype>? damageGroup,
        WoundableComponent? woundable = null)
    {
        woundCreated = null;

        if (_net.IsClient
            || !IsWoundPrototypeValid(woundProtoId)
            || !Resolve(uid, ref woundable))
            return false;

        var wound = Spawn(WoundPrototypeId(woundProtoId));
        if (AddWound(uid, wound, severity, damageGroup, woundable))
        {
            woundCreated = (wound, Comp<WoundComponent>(wound));
            return true;
        }

        QueueDel(wound);
        return false;
    }

    public bool TryContinueWound(
        EntityUid uid,
        string id,
        FixedPoint2 severity,
        [NotNullWhen(true)] out Entity<WoundComponent>? woundContinued,
        WoundableComponent? woundable = null)
    {
        woundContinued = null;
        if (!IsWoundPrototypeValid(id)
            || !Resolve(uid, ref woundable))
            return false;

        foreach (var wound in GetWoundableWounds(uid, woundable))
        {
            if (id != wound.Comp.DamageType || wound.Comp.IsScar)
                continue;

            ApplyWoundSeverity(wound, severity, wound.Comp, woundable);
            UpdateWoundableIntegrity(uid, woundable);
            CheckWoundableSeverityThresholds(uid, woundable);
            woundContinued = wound;
            return true;
        }

        return false;
    }

    private bool AddWound(
        EntityUid target,
        EntityUid wound,
        FixedPoint2 woundSeverity,
        ProtoId<DamageGroupPrototype>? damageGroup,
        WoundableComponent woundable)
    {
        if (!_woundQuery.TryComp(wound, out var woundComp)
            || woundable.Wounds == null
            || woundable.Wounds.Contains(wound)
            || !woundable.AllowWounds)
            return false;

        woundComp.HoldingWoundable = target;
        woundComp.DamageGroup = damageGroup;

        if (!_container.Insert(wound, woundable.Wounds))
            return false;

        SetWoundSeverity(wound, woundSeverity, woundComp, woundable);

        Dirty(wound, woundComp);
        Dirty(target, woundable);
        return true;
    }

    private bool RemoveWound(EntityUid woundEntity, WoundComponent? wound = null)
    {
        if (_net.IsClient
            || !Resolve(woundEntity, ref wound, false)
            || !_woundableQuery.TryComp(wound.HoldingWoundable, out var woundable)
            || woundable.Wounds == null)
            return false;

        var holding = wound.HoldingWoundable;
        wound.HoldingWoundable = EntityUid.Invalid;

        _container.Remove(woundEntity, woundable.Wounds, false, true);
        QueueDel(woundEntity);

        UpdateWoundableIntegrity(holding, woundable);
        CheckWoundableSeverityThresholds(holding, woundable);
        return true;
    }

    #endregion

    #region Severity and integrity

    public void SetWoundSeverity(
        EntityUid uid,
        FixedPoint2 severity,
        WoundComponent? wound = null,
        WoundableComponent? woundable = null)
    {
        if (!Resolve(uid, ref wound)
            || !Resolve(wound.HoldingWoundable, ref woundable))
            return;

        var old = wound.WoundSeverityPoint;
        var holding = wound.HoldingWoundable;

        var upperLimit = wound.WoundSeverityPoint + woundable.WoundableIntegrity;
        wound.WoundSeverityPoint = FixedPoint2.Clamp(ApplySeverityModifiers(holding, severity, woundable), 0, upperLimit);

        if (wound.WoundSeverityPoint != old)
        {
            var ev = new WoundSeverityPointChangedEvent(wound, old, wound.WoundSeverityPoint);
            RaiseLocalEvent(uid, ref ev);
        }

        if (TerminatingOrDeleted(uid) || wound.HoldingWoundable != holding)
            return;

        CheckSeverityThresholds(uid, holding, wound, woundable);
        Dirty(uid, wound);

        UpdateWoundableIntegrity(holding, woundable);
        CheckWoundableSeverityThresholds(holding, woundable);
    }

    /// <summary>
    /// Applies severity to a wound. Does not update the woundable's integrity, callers do that once all changes are done.
    /// </summary>
    public void ApplyWoundSeverity(
        EntityUid uid,
        FixedPoint2 severity,
        WoundComponent? wound = null,
        WoundableComponent? woundable = null)
    {
        if (!Resolve(uid, ref wound)
            || !Resolve(wound.HoldingWoundable, ref woundable))
            return;

        var old = wound.WoundSeverityPoint;
        var holding = wound.HoldingWoundable;
        var rawValue = severity > 0
            ? old + ApplySeverityModifiers(holding, severity, woundable)
            : old + severity;

        var upperLimit = wound.WoundSeverityPoint + woundable.WoundableIntegrity;
        wound.WoundSeverityPoint = FixedPoint2.Clamp(rawValue, 0, upperLimit);
        Dirty(uid, wound);

        if (wound.WoundSeverityPoint != old || rawValue > wound.WoundSeverityPoint)
        {
            FixedPoint2? overflow = rawValue > wound.WoundSeverityPoint ? rawValue - wound.WoundSeverityPoint : null;
            var ev = new WoundSeverityPointChangedEvent(wound, old, wound.WoundSeverityPoint, overflow);
            RaiseLocalEvent(uid, ref ev);
        }

        if (TerminatingOrDeleted(uid) || wound.HoldingWoundable != holding)
            return;

        CheckSeverityThresholds(uid, holding, wound, woundable);
    }

    public FixedPoint2 ApplySeverityModifiers(EntityUid woundable, FixedPoint2 severity, WoundableComponent? component = null)
    {
        if (!Resolve(woundable, ref component) || component.SeverityMultipliers.Count == 0)
            return severity;

        var sum = 0f;
        foreach (var multiplier in component.SeverityMultipliers)
            sum += (float) multiplier.Value.Change;

        return severity * (sum / component.SeverityMultipliers.Count);
    }

    private void CheckSeverityThresholds(
        EntityUid wound,
        EntityUid woundable,
        WoundComponent? component = null,
        WoundableComponent? woundableComp = null)
    {
        if (!Resolve(wound, ref component, false)
            || !Resolve(woundable, ref woundableComp))
            return;

        var nearestSeverity = component.WoundSeverity;
        foreach (var (severity, value) in WoundThresholds)
        {
            var scaledThreshold = value * (woundableComp.IntegrityCap / 100);
            if (component.WoundSeverityPoint < scaledThreshold)
                continue;

            if (severity == WoundSeverity.Healed && component.WoundSeverityPoint > 0)
                continue;

            nearestSeverity = severity;
            break;
        }

        if (nearestSeverity == component.WoundSeverity)
            return;

        var ev = new WoundSeverityChangedEvent(component.WoundSeverity, nearestSeverity);
        component.WoundSeverity = nearestSeverity;
        RaiseLocalEvent(wound, ref ev);
    }

    private void CheckWoundableSeverityThresholds(EntityUid woundable, WoundableComponent? component = null)
    {
        if (!Resolve(woundable, ref component, false))
            return;

        component.SortedThresholds ??= [.. component.Thresholds.OrderByDescending(kv => kv.Value)];
        var nearestSeverity = component.WoundableSeverity;
        foreach (var (severity, value) in component.SortedThresholds)
        {
            if (component.WoundableIntegrity >= component.IntegrityCap)
            {
                nearestSeverity = WoundableSeverity.Healthy;
                break;
            }

            if (component.WoundableIntegrity < value)
                continue;

            nearestSeverity = severity;
            break;
        }

        if (nearestSeverity == component.WoundableSeverity)
            return;

        var ev = new WoundableSeverityChangedEvent(component.WoundableSeverity, nearestSeverity);
        RaiseLocalEvent(woundable, ref ev);
        component.WoundableSeverity = nearestSeverity;

        Dirty(woundable, component);
    }

    public void UpdateWoundableIntegrity(EntityUid uid, WoundableComponent? component = null)
    {
        if (!Resolve(uid, ref component, false)
            || component.Wounds == null)
            return;

        var damage = FixedPoint2.Zero;
        foreach (var wound in GetWoundableWounds(uid, component))
        {
            if (!wound.Comp.IsScar)
                damage += wound.Comp.WoundIntegrityDamage;
        }

        var newIntegrity = FixedPoint2.Clamp(component.IntegrityCap - damage, 0, component.IntegrityCap);
        if (newIntegrity == component.WoundableIntegrity)
            return;

        var ev = new WoundableIntegrityChangedEvent(component.WoundableIntegrity, newIntegrity);
        RaiseLocalEvent(uid, ref ev);

        component.WoundableIntegrity = newIntegrity;
        Dirty(uid, component);
    }

    #endregion
}
