// SPDX-License-Identifier: MIT

using System.Numerics;
// using Content.Shared._Omu.Paper; // Inferus - no paper signatures
using Content.Shared._Omu.Crewlet;
// using Content.Server._DV.Paper; // Inferus - no paper signatures
using Content.Server.Chat.Systems;
using Content.Server.NPC.Components;
using Content.Server.NPC.Systems;
using Content.Server.VendingMachines;
using Content.Server.Verbs;
using Content.Shared.Access.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Anomaly.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Buckle;
using Content.Shared.Buckle.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Climbing.Components;
using Content.Shared.CombatMode;
using Content.Shared.Cuffs;
using Content.Shared.Damage.Components; // Inferus
using Content.Shared.Damage.Systems; // Inferus
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Examine;
using Content.Shared.Fluids;
using Content.Shared.Fluids.Components;
using Content.Shared.Ghost;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Item;
using Content.Shared.Maps;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Pointing;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Content.Shared.Sound.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.VendingMachines;
using Content.Shared.Wall;
using Content.Shared.Weapons.Melee;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

using Content.Shared.Ghost.Components; // Inferus
using Content.Shared.VendingMachines.Components; // Inferus

namespace Content.Server._Omu.Crewlet;

public sealed partial class CrewletBrainSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!; // Inferus
    // [Dependency] private IMapManager _mapManager = default!; // Inferus - IMapManager removed
    [Dependency] private IPrototypeManager _proto = default!; // Inferus
    [Dependency] private IRobustRandom _random = default!; // Inferus
    [Dependency] private AccessReaderSystem _accessReader = default!; // Inferus
    [Dependency] private ActionBlockerSystem _actionBlocker = default!; // Inferus
    [Dependency] private ChatSystem _chat = default!; // Inferus
    // [Dependency] private CommonSignatureFontSystem _signatureFont = default!; // Inferus - no paper signatures
    [Dependency] private CrewletMergeSystem _merge = default!; // Inferus
    [Dependency] private CrewletReputationSystem _reputation = default!; // Inferus
    [Dependency] private CrewletSystem _crewlet = default!; // Inferus
    [Dependency] private EntityLookupSystem _lookup = default!; // Inferus
    [Dependency] private EntityWhitelistSystem _whitelist = default!; // Inferus
    [Dependency] private ExamineSystemShared _examine = default!; // Inferus
    [Dependency] private IngestionSystem _ingestion = default!; // Inferus
    [Dependency] private InventorySystem _inventory = default!; // Inferus
    [Dependency] private MobStateSystem _mobState = default!; // Inferus
    [Dependency] private SatiationSystem _satiation = default!; // Inferus - satiation refactor
    [Dependency] private NPCSteeringSystem _steering = default!; // Inferus
    [Dependency] private PullingSystem _pulling = default!; // Inferus
    [Dependency] private RotateToFaceSystem _rotate = default!; // Inferus
    [Dependency] private SharedAppearanceSystem _appearance = default!; // Inferus
    [Dependency] private SharedAudioSystem _audio = default!; // Inferus
    [Dependency] private SharedBuckleSystem _buckle = default!; // Inferus
    [Dependency] private SharedCombatModeSystem _combat = default!; // Inferus
    [Dependency] private SharedContainerSystem _container = default!; // Inferus
    [Dependency] private SharedCuffableSystem _cuffable = default!; // Inferus
    [Dependency] private SharedDoAfterSystem _doAfter = default!; // Inferus
    [Dependency] private SharedEntityStorageSystem _entityStorage = default!; // Inferus
    [Dependency] private SharedHandsSystem _hands = default!; // Inferus
    [Dependency] private SharedIdCardSystem _idCard = default!; // Inferus
    [Dependency] private SharedInteractionSystem _interaction = default!; // Inferus
    [Dependency] private SharedMapSystem _map = default!; // Inferus
    [Dependency] private SharedMeleeWeaponSystem _melee = default!; // Inferus
    [Dependency] private SharedMoverController _mover = default!; // Inferus
    [Dependency] private SharedPointingSystem _pointing = default!; // Inferus
    [Dependency] private SharedPopupSystem _popup = default!; // Inferus
    [Dependency] private SharedPowerReceiverSystem _power = default!; // Inferus
    [Dependency] private SharedPuddleSystem _puddle = default!; // Inferus
    [Dependency] private SharedSolutionContainerSystem _solutions = default!; // Inferus
    [Dependency] private SharedStaminaSystem _stamina = default!; // Inferus
    [Dependency] private SharedStunSystem _stun = default!; // Inferus
    [Dependency] private SharedTransformSystem _transform = default!; // Inferus
    // [Dependency] private SignatureSystem _signature = default!; // Inferus - no paper signatures
    [Dependency] private SleepingSystem _sleeping = default!; // Inferus
    [Dependency] private TurfSystem _turf = default!; // Inferus
    [Dependency] private VendingMachineSystem _vending = default!; // Inferus
    [Dependency] private VerbSystem _verbs = default!; // Inferus

    private EntityQuery<AnomalyComponent> _anomalyQuery;
    private EntityQuery<ClimbableComponent> _climbableQuery;
    private EntityQuery<CrewletComponent> _crewletQuery;
    private EntityQuery<DoorComponent> _doorQuery;
    private EntityQuery<EdibleComponent> _edibleQuery;
    private EntityQuery<EmitSoundOnUseComponent> _hornQuery;
    private EntityQuery<EntityStorageComponent> _entityStorageQuery;
    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<HumanoidProfileComponent> _humanoidQuery;
    private EntityQuery<InteractionPopupComponent> _popupQuery;
    private EntityQuery<ItemComponent> _itemQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<CrewletOwnerComponent> _ownerQuery;
    private EntityQuery<NPCSteeringComponent> _steeringQuery;
    private EntityQuery<PuddleComponent> _puddleQuery;
    private EntityQuery<SleepingComponent> _sleepingQuery;
    private EntityQuery<StrapComponent> _strapQuery;
    private EntityQuery<VendingMachineComponent> _vendingQuery;
    private EntityQuery<VirtualItemComponent> _virtualItemQuery;
    private EntityQuery<WallMountComponent> _wallMountQuery;

    public override void Initialize()
    {
        base.Initialize();

        UpdatesAfter.Add(typeof(NPCSteeringSystem));
        UpdatesBefore.Add(typeof(SharedPhysicsSystem));

        InitializeReactions();
        InitializeMerge();
        SubscribeLocalEvent<CrewletBrainComponent, MindAddedMessage>(OnMindAdded);

        _anomalyQuery = GetEntityQuery<AnomalyComponent>();
        _climbableQuery = GetEntityQuery<ClimbableComponent>();
        _crewletQuery = GetEntityQuery<CrewletComponent>();
        _doorQuery = GetEntityQuery<DoorComponent>();
        _edibleQuery = GetEntityQuery<EdibleComponent>();
        _hornQuery = GetEntityQuery<EmitSoundOnUseComponent>();
        _entityStorageQuery = GetEntityQuery<EntityStorageComponent>();
        _ghostQuery = GetEntityQuery<GhostComponent>();
        _humanoidQuery = GetEntityQuery<HumanoidProfileComponent>();
        _popupQuery = GetEntityQuery<InteractionPopupComponent>();
        _itemQuery = GetEntityQuery<ItemComponent>();
        _mobStateQuery = GetEntityQuery<MobStateComponent>();
        _ownerQuery = GetEntityQuery<CrewletOwnerComponent>();
        _steeringQuery = GetEntityQuery<NPCSteeringComponent>();
        _puddleQuery = GetEntityQuery<PuddleComponent>();
        _sleepingQuery = GetEntityQuery<SleepingComponent>();
        _strapQuery = GetEntityQuery<StrapComponent>();
        _vendingQuery = GetEntityQuery<VendingMachineComponent>();
        _virtualItemQuery = GetEntityQuery<VirtualItemComponent>();
        _wallMountQuery = GetEntityQuery<WallMountComponent>();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<CrewletBrainSystem>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Inferus - In Inferus the Walk key is held to sprint, so crewlets sprint like players do:
        // they run by default, walk while keeping away from someone or near their owner, drain stamina
        // while sprinting and slow to a walk when winded until they catch their breath.
        // using var query = EntityQueryEnumerator<NPCSteeringComponent, CrewletBrainComponent, InputMoverComponent>();
        // while (query.MoveNext(out _, out _, out _, out var mover))
        // {
        //     if (mover.Sprinting)
        //         continue;
        //
        //     mover.CurTickWalkMovement = mover.CurTickSprintMovement;
        //     mover.CurTickSprintMovement = Vector2.Zero;
        // }
        using var query = EntityQueryEnumerator<NPCSteeringComponent, CrewletBrainComponent, InputMoverComponent>();
        while (query.MoveNext(out var uid, out _, out var brain, out var mover))
        {
            var stamina = CompOrNull<StaminaComponent>(uid);
            if (stamina != null)
            {
                if (stamina.StaminaDamage >= stamina.CritThreshold * brain.WindedThreshold)
                    brain.Winded = true;
                else if (stamina.StaminaDamage <= stamina.CritThreshold * brain.RecoveredThreshold)
                    brain.Winded = false;
            }

            var nearOwner = brain.KnownOwner is { } owner
                            && !TerminatingOrDeleted(owner)
                            && DistanceTo(uid, owner) <= brain.OwnerWalkRange;
            var sprint = brain.Active && brain.Activity != CrewletActivity.KeepAway && !brain.Winded && !nearOwner;
            SetSprint((uid, mover), sprint);

            if (mover.Sprinting)
            {
                if (stamina != null && mover.CurTickSprintMovement != Vector2.Zero)
                    _stamina.TakeStaminaDamage(uid, stamina.SprintDrainPerSecond * frameTime, stamina, visual: false);

                continue;
            }

            mover.CurTickWalkMovement = mover.CurTickSprintMovement;
            mover.CurTickSprintMovement = Vector2.Zero;
        }
        // End Inferus
    }

    public void StartThinking(EntityUid uid)
    {
        if (!TryComp<CrewletBrainComponent>(uid, out var brain))
            return;

        var now = _timing.CurTime;
        brain.Active = true;
        brain.Activity = CrewletActivity.None;
        brain.Step = CrewletStep.Start;
        brain.Target = null;
        brain.NextScan = now + _random.Next(brain.ScanInterval);
        brain.NextVocalize = now + _random.Next(brain.MinVocalizeInterval, brain.MaxVocalizeInterval);
        brain.NextTypingRoll = now + _random.Next(brain.TypingRollInterval);
        brain.NextWakeRoll = now + _random.Next(brain.WakeRollInterval);
    }

    public void Think(EntityUid uid)
    {
        if (!TryComp<CrewletBrainComponent>(uid, out var brain) || !brain.Active)
            return;

        var ent = new Entity<CrewletBrainComponent>(uid, brain);
        var now = _timing.CurTime;

        TrackOwner(ent, now);
        Perceive(ent, now);
        UpdateWatches(ent, now);
        UpdateSpeech(ent, now);
        UpdateParrot(ent, now);
        RollWake(ent, now);
        Choose(ent, now);
        Run(ent, now);
    }

    private void OnMindAdded(Entity<CrewletBrainComponent> ent, ref MindAddedMessage args)
    {
        StopThinking(ent);
    }

    public void StopThinking(EntityUid uid)
    {
        if (!TryComp<CrewletBrainComponent>(uid, out var brain) || !brain.Active)
            return;

        var ent = new Entity<CrewletBrainComponent>(uid, brain);
        End(ent);
        StopTyping(ent);
        brain.PendingLine = null;
        brain.Active = false;
    }

    private void Choose(Entity<CrewletBrainComponent> ent, TimeSpan now)
    {
        var next = Triggered(ent, now);
        if (next != ent.Comp.Activity)
            Switch(ent, next, now);
        else if (next == CrewletActivity.None)
            Switch(ent, CrewletActivity.Wander, now);
    }

    private CrewletActivity Triggered(Entity<CrewletBrainComponent> ent, TimeSpan now)
    {
        var current = ent.Comp.Activity;

        if (current > CrewletActivity.Escape && WantsEscape(ent, now))
            return CrewletActivity.Escape;

        if (current > CrewletActivity.Unbuckle && WantsUnbuckle(ent, now))
            return CrewletActivity.Unbuckle;

        if (current > CrewletActivity.BreakPull && WantsBreakPull(ent, now))
            return CrewletActivity.BreakPull;

        if (current > CrewletActivity.Retaliate && WantsRetaliate(ent, now))
            return CrewletActivity.Retaliate;

        if (current > CrewletActivity.Rest && now < ent.Comp.RestUntil)
            return CrewletActivity.Rest;

        if (current > CrewletActivity.Stay && now < ent.Comp.StayUntil)
            return CrewletActivity.Stay;

        if (current > CrewletActivity.Merge && WantsMerge(ent, now))
            return CrewletActivity.Merge;

        if (current > CrewletActivity.Recall && WantsRecall(ent, now))
            return CrewletActivity.Recall;

        if (current > CrewletActivity.CatchUp && WantsCatchUp(ent, now))
            return CrewletActivity.CatchUp;

        if (current > CrewletActivity.Mourn && WantsMourn(ent, now))
            return CrewletActivity.Mourn;

        if (current > CrewletActivity.Worry && WantsWorry(ent, now))
            return CrewletActivity.Worry;

        if (current > CrewletActivity.Deliver && WantsDeliver(ent, now))
            return CrewletActivity.Deliver;

        if (current > CrewletActivity.Fetch && WantsFetch(ent, now))
            return CrewletActivity.Fetch;

        if (current > CrewletActivity.HugReaction && WantsHugReaction(ent, now))
            return CrewletActivity.HugReaction;

        if (current > CrewletActivity.Stare && WantsStare(ent, now))
            return CrewletActivity.Stare;

        if (current > CrewletActivity.Dance && WantsDance(ent, now))
            return CrewletActivity.Dance;

        if (current > CrewletActivity.Greet && WantsGreet(ent, now))
            return CrewletActivity.Greet;

        if (current > CrewletActivity.Hello && WantsHello(ent, now))
            return CrewletActivity.Hello;

        if (current > CrewletActivity.WakeSleeper && WantsWakeSleeper(ent, now))
            return CrewletActivity.WakeSleeper;

        if (IdleTier(ent.Comp) && WantsTagged(ent))
            return CrewletActivity.Tag;

        if (current > CrewletActivity.Follow && WantsFollow(ent, now))
            return CrewletActivity.Follow;

        if (current > CrewletActivity.Eat && WantsEat(ent, now))
            return CrewletActivity.Eat;

        if (current > CrewletActivity.KeepAway && WantsKeepAway(ent, now))
            return CrewletActivity.KeepAway;

        return current;
    }

    private void Run(Entity<CrewletBrainComponent> ent, TimeSpan now)
    {
        switch (ent.Comp.Activity)
        {
            case CrewletActivity.Escape:
                RunEscape(ent, now);
                break;
            case CrewletActivity.Unbuckle:
                RunUnbuckle(ent, now);
                break;
            case CrewletActivity.BreakPull:
                RunBreakPull(ent, now);
                break;
            case CrewletActivity.Retaliate:
                RunRetaliate(ent, now);
                break;
            case CrewletActivity.Rest:
                RunRest(ent, now);
                break;
            case CrewletActivity.Stay:
                RunStay(ent, now);
                break;
            case CrewletActivity.Merge:
                RunMerge(ent, now);
                break;
            case CrewletActivity.Recall:
                RunRecall(ent, now);
                break;
            case CrewletActivity.CatchUp:
            case CrewletActivity.Follow:
                RunFollow(ent, now);
                break;
            case CrewletActivity.Mourn:
                RunMourn(ent, now);
                break;
            case CrewletActivity.Worry:
                RunWorry(ent, now);
                break;
            case CrewletActivity.Hello:
                RunHello(ent, now);
                break;
            case CrewletActivity.Deliver:
                RunDeliver(ent, now);
                break;
            case CrewletActivity.Fetch:
                RunFetch(ent, now);
                break;
            case CrewletActivity.HugReaction:
                RunHugReaction(ent, now);
                break;
            case CrewletActivity.Stare:
                RunStare(ent, now);
                break;
            case CrewletActivity.Dance:
                RunDance(ent, now);
                break;
            case CrewletActivity.Greet:
                RunGreet(ent, now);
                break;
            case CrewletActivity.WakeSleeper:
                RunWakeSleeper(ent, now);
                break;
            case CrewletActivity.KeepAway:
                RunKeepAway(ent, now);
                break;
            case CrewletActivity.Hug:
                RunHug(ent, now);
                break;
            case CrewletActivity.Loot:
                RunLoot(ent, now);
                break;
            case CrewletActivity.Hat:
                RunHat(ent, now);
                break;
            case CrewletActivity.Climb:
                RunClimb(ent, now);
                break;
            case CrewletActivity.Knock:
                RunKnock(ent, now);
                break;
            case CrewletActivity.Punch:
                RunPunch(ent, now);
                break;
            case CrewletActivity.Pickpocket:
                RunPickpocket(ent, now);
                break;
            case CrewletActivity.Pet:
                RunPet(ent, now);
                break;
            case CrewletActivity.Glance:
                RunGlance(ent, now);
                break;
            case CrewletActivity.Sit:
                RunSit(ent, now);
                break;
            case CrewletActivity.Honk:
                RunHonk(ent, now);
                break;
            case CrewletActivity.Mop:
                RunMop(ent, now);
                break;
            case CrewletActivity.Mimic:
                RunMimic(ent);
                break;
            case CrewletActivity.Tag:
                RunTag(ent, now);
                break;
            case CrewletActivity.Tinker:
            case CrewletActivity.Habit:
                RunTinker(ent, now);
                break;
            case CrewletActivity.Eat:
                RunEat(ent, now);
                break;
            case CrewletActivity.Wander:
                RunWander(ent, now);
                break;
            case CrewletActivity.Idle:
                RunIdle(ent, now);
                break;
        }
    }

    private void Switch(Entity<CrewletBrainComponent> ent, CrewletActivity next, TimeSpan now)
    {
        var target = ent.Comp.Candidate;
        End(ent);

        ent.Comp.Activity = next;
        ent.Comp.Target = target;

        if (ent.Comp.ActivityCooldowns.TryGetValue(next, out var cooldown))
            ent.Comp.ActivityReadyAt[next] = now + cooldown;
    }

    private void End(Entity<CrewletBrainComponent> ent)
    {
        StopMoving(ent);

        // Inferus - sprinting is handled in Update
        // if (ent.Comp.Activity == CrewletActivity.KeepAway)
        //     SetWalking(ent, false);
        if (TryComp<InputMoverComponent>(ent, out var mover))
            SetSprint((ent, mover), false);
        // End Inferus

        if (ent.Comp.ActivityDoAfter is { } doAfter)
        {
            ent.Comp.ActivityDoAfter = null;
            if (_doAfter.GetStatus(doAfter) == DoAfterStatus.Running)
                _doAfter.Cancel(doAfter);
        }

        if (ent.Comp.FormedTool is { } formed)
        {
            ent.Comp.FormedTool = null;
            QueueDel(formed);
        }
        else if (ent.Comp.Activity is CrewletActivity.Tinker or CrewletActivity.Habit
            && ent.Comp.RetaliationTarget == null
            && ent.Comp.Target is { } tool
            && _hands.IsHolding(ent.Owner, tool))
            _hands.TryDrop(ent.Owner, tool);

        if (ent.Comp.InCombat)
        {
            ent.Comp.InCombat = false;
            _combat.SetInCombatMode(ent, false);
        }

        if (HeldItem(ent) == null)
            ent.Comp.GiftRecipient = null;

        ent.Comp.Activity = CrewletActivity.None;
        ent.Comp.Step = CrewletStep.Start;
        ent.Comp.Target = null;
        ent.Comp.Candidate = null;
        ent.Comp.Count = 0;
    }

    private void Fail(Entity<CrewletBrainComponent> ent, TimeSpan now)
    {
        var activity = ent.Comp.Activity;
        var retry = now + ent.Comp.FailureCooldown;
        if (!ent.Comp.ActivityReadyAt.TryGetValue(activity, out var ready) || ready < retry)
            ent.Comp.ActivityReadyAt[activity] = retry;

        End(ent);
    }

    private bool IsReady(CrewletBrainComponent brain, CrewletActivity activity, TimeSpan now)
    {
        return !brain.ActivityReadyAt.TryGetValue(activity, out var ready) || now >= ready;
    }

    private void WaitFor(Entity<CrewletBrainComponent> ent, TimeSpan duration, TimeSpan now)
    {
        StopMoving(ent);
        ent.Comp.Step = CrewletStep.Wait;
        ent.Comp.StepEnds = now + duration;
    }

    private void Interact(Entity<CrewletBrainComponent> ent, EntityUid target)
    {
        if (_combat.IsInCombatMode(ent))
            _combat.SetInCombatMode(ent, false);

        ent.Comp.InCombat = false;
        _interaction.UserInteraction(ent, Transform(target).Coordinates, target);
    }

    private bool Present(EntityUid? uid)
    {
        return uid is { } target && !TerminatingOrDeleted(target);
    }
}
