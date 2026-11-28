// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/SharedSurgerySystem*.cs) // Inferus
// Inferus - a server-only rewrite of the core of Goob's surgery system for the organ based body. Only what the first stage needs:
// surgeries and steps as prototype entities, tool checks, do-afters, component based step state, and the data for the window.
// Left out for now, they come back with the surgeries that need them: armor checks, operating tables, sanitation, organ and part steps.
using Content.Shared._Shitmed.Surgery.Consciousness;
using Content.Shared._Shitmed.Surgery.Operations;
using Content.Shared._Shitmed.Surgery.Pain;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Bed.Sleep;
using Content.Shared.Body;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Verbs;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Shitmed.Surgery;

public sealed partial class SurgerySystem : EntitySystem
{
    private const float Range = 2.5f;

    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private PainSystem _pain = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StandingStateSystem _standing = default!;

    /// <summary>
    /// The entity every surgery and step prototype is spawned as once, to read its components and raise events on it.
    /// </summary>
    private readonly Dictionary<EntProtoId, EntityUid> _singletons = new();

    private readonly List<EntProtoId> _surgeries = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SurgeryStepChosenEvent>(OnStepChosen);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        LoadPrototypes();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<EntityPrototype>())
            LoadPrototypes();
    }

    private void LoadPrototypes()
    {
        foreach (var uid in _singletons.Values)
        {
            if (!TerminatingOrDeleted(uid))
                Del(uid);
        }

        _singletons.Clear();
        _surgeries.Clear();

        foreach (var proto in _prototypes.EnumeratePrototypes<EntityPrototype>())
        {
            if (!proto.Abstract && proto.TryGetComponent<SurgeryComponent>(out _, _factory))
                _surgeries.Add(new EntProtoId(proto.ID));
        }

        Log.Info($"Surgery: found {_surgeries.Count} surgery prototypes.");
    }

    private EntityUid? GetSingleton(EntProtoId id)
    {
        if (!_prototypes.HasIndex(id))
            return null;

        if (!_singletons.TryGetValue(id, out var uid) || TerminatingOrDeleted(uid))
        {
            uid = Spawn(id, MapCoordinates.Nullspace);
            _singletons[id] = uid;
        }

        return uid;
    }

    #region Conditions

    [SubscribeLocalEvent]
    private void OnPartComponentCondition(Entity<SurgeryPartComponentConditionComponent> ent, ref SurgeryValidEvent args)
    {
        var present = true;
        foreach (var reg in ent.Comp.Components.Values)
        {
            if (!HasComp(args.Part, reg.Component.GetType()))
                present = false;
        }

        args.Cancelled |= present == ent.Comp.Inverse;
    }

    #endregion

    #region Starting

    [SubscribeLocalEvent]
    private void OnToolVerb(Entity<SurgeryToolComponent> tool, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !HasComp<WoundDamageModelComponent>(args.Target))
            return;

        var user = args.User;
        var target = args.Target;

        args.Verbs.Add(new UtilityVerb
        {
            Act = () => TryOpen(user, target),
            Text = Loc.GetString("surgery-verb-text"),
            Message = Loc.GetString("surgery-verb-message"),
            DoContactInteraction = true,
        });
    }

    private void TryOpen(EntityUid user, EntityUid body)
    {
        if (!CanOperate(user, body))
            return;

        SendState(user, body);
    }

    private bool CanOperate(EntityUid user, EntityUid body)
    {
        if (user == body)
        {
            _popup.PopupEntity(Loc.GetString("surgery-error-self-surgery"), user, user);
            return false;
        }

        if (!InRange(user, body))
            return false;

        var down = _standing.IsDown(body) || HasComp<SleepingComponent>(body);
        if (!down)
            _popup.PopupEntity(Loc.GetString("surgery-error-laying"), user, user);

        return down;
    }

    private bool InRange(EntityUid user, EntityUid body)
    {
        return _transform.InRange(Transform(user).Coordinates, Transform(body).Coordinates, Range);
    }

    #endregion

    #region Steps

    private bool IsSurgeryValid(EntityUid body, EntityUid part, EntProtoId id, out Entity<SurgeryComponent> surgery)
    {
        surgery = default;

        if (GetSingleton(id) is not { } uid || !TryComp<SurgeryComponent>(uid, out var comp))
        {
            Log.Warning($"Surgery: could not get the surgery entity for {id}.");
            return false;
        }

        var ev = new SurgeryValidEvent(body, part);
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            return false;

        // Inferus - the requirement only has to be done, it does not have to still be on offer (opening an incision hides itself once done)
        if (comp.Requirement is { } requirement && !IsSurgeryComplete(body, part, requirement))
            return false;

        surgery = (uid, comp);
        return true;
    }

    private bool IsStepComplete(EntityUid body, EntityUid part, EntityUid surgery, EntityUid stepEnt)
    {
        if (!TryComp<SurgeryStepComponent>(stepEnt, out var step))
            return false;

        if (!HasAll(part, step.Add) || HasAny(part, step.Remove) || !HasAll(body, step.BodyAdd) || HasAny(body, step.BodyRemove))
            return false;

        var ev = new SurgeryStepCompleteCheckEvent(body, part, surgery);
        RaiseLocalEvent(stepEnt, ref ev);
        return !ev.Cancelled;
    }

    private bool HasAll(EntityUid uid, ComponentRegistry? registry)
    {
        if (registry == null)
            return true;

        foreach (var reg in registry.Values)
        {
            if (!HasComp(uid, reg.Component.GetType()))
                return false;
        }

        return true;
    }

    private bool HasAny(EntityUid uid, ComponentRegistry? registry)
    {
        if (registry == null)
            return false;

        foreach (var reg in registry.Values)
        {
            if (HasComp(uid, reg.Component.GetType()))
                return true;
        }

        return false;
    }

    /// <summary>
    /// The index of the first step that is not done yet, or -1 when all are.
    /// </summary>
    private int GetNextStep(EntityUid body, EntityUid part, Entity<SurgeryComponent> surgery)
    {
        for (var i = 0; i < surgery.Comp.Steps.Count; i++)
        {
            if (GetSingleton(surgery.Comp.Steps[i]) is not { } step || !IsStepComplete(body, part, surgery, step))
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Checks the held item against the tool the step wants. Returns the tool component it matched, if any.
    /// </summary>
    private bool CheckTool(EntityUid user, EntityUid tool, SurgeryStepComponent step, out ISurgeryToolComponent? matched, bool popup)
    {
        matched = null;

        if (step.Tool == null)
            return true;

        foreach (var reg in step.Tool.Values)
        {
            if (EntityManager.TryGetComponent(tool, reg.Component.GetType(), out var comp) && comp is ISurgeryToolComponent toolComp)
            {
                matched = toolComp;
                return true;
            }

            if (popup && reg.Component is ISurgeryToolComponent required)
                _popup.PopupEntity(Loc.GetString("surgery-error-missing-tool", ("tool", required.ToolName)), user, user);

            return false;
        }

        return true;
    }

    private void OnStepChosen(SurgeryStepChosenEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user)
            return;

        var body = GetEntity(msg.Body);
        var part = GetEntity(msg.Part);

        if (Deleted(body) || Deleted(part)
            || !HasComp<WoundDamageModelComponent>(body)
            || !TryComp<OrganComponent>(part, out var organ) || organ.Body != body
            || !CanOperate(user, body))
            return;

        if (!IsSurgeryValid(body, part, msg.Surgery, out var surgery))
            return;

        var next = GetNextStep(body, part, surgery);
        if (next < 0 || surgery.Comp.Steps[next] != msg.Step || GetSingleton(msg.Step) is not { } stepEnt)
            return;

        var stepEv = new SurgeryValidEvent(body, part);
        RaiseLocalEvent(stepEnt, ref stepEv);
        if (stepEv.Cancelled || !TryComp<SurgeryStepComponent>(stepEnt, out var step))
            return;

        var tool = _hands.GetActiveItemOrSelf(user);
        if (!CheckTool(user, tool, step, out var matched, popup: true))
            return;

        var duration = step.Duration / Math.Max(0.1f, matched?.Speed ?? 1f);
        var ev = new SurgeryDoAfterEvent(msg.Surgery, msg.Step, msg.Part);
        var doAfter = new DoAfterArgs(EntityManager, user, duration, ev, body, target: body, used: tool == user ? null : tool)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    [SubscribeLocalEvent]
    private void OnDoAfter(Entity<WoundDamageModelComponent> body, ref SurgeryDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        args.Handled = true;

        var user = args.User;
        var part = GetEntity(args.Part);
        var tool = args.Used ?? user;

        if (Deleted(part)
            || !InRange(user, body)
            || !IsSurgeryValid(body, part, args.Surgery, out var surgery))
            return;

        var next = GetNextStep(body, part, surgery);
        if (next < 0 || surgery.Comp.Steps[next] != args.Step || GetSingleton(args.Step) is not { } stepEnt)
            return;

        var validEv = new SurgeryValidEvent(body, part);
        RaiseLocalEvent(stepEnt, ref validEv);
        if (validEv.Cancelled || !TryComp<SurgeryStepComponent>(stepEnt, out var step) || !CheckTool(user, tool, step, out _, popup: true))
            return;

        if (step.Add != null)
            EntityManager.AddComponents(part, step.Add);

        if (step.Remove != null)
            EntityManager.RemoveComponents(part, step.Remove);

        if (step.BodyAdd != null)
            EntityManager.AddComponents(body, step.BodyAdd);

        if (step.BodyRemove != null)
            EntityManager.RemoveComponents(body, step.BodyRemove);

        var stepEv = new SurgeryStepEvent(user, body, part, tool, surgery, stepEnt);
        RaiseLocalEvent(stepEnt, ref stepEv);

        // Inferus - a repeatable step starts again by itself until it is done, so tending wounds is one click
        args.Repeat = HasComp<SurgeryRepeatableStepComponent>(stepEnt) && !IsStepComplete(body, part, surgery, stepEnt);

        if (step.Pain > 0f && IsAwake(body))
        {
            var pain = EnsureComp<PainComponent>(body);
            _pain.AddPain((body, pain), step.Pain);
        }

        SendState(user, body);
    }

    private bool IsAwake(EntityUid body)
    {
        if (HasComp<SleepingComponent>(body))
            return false;

        return !TryComp<ConsciousnessComponent>(body, out var consciousness) || !consciousness.Unconscious;
    }

    #endregion

    #region Window

    private void SendState(EntityUid user, EntityUid body)
    {
        if (!TryComp<ActorComponent>(user, out var actor))
            return;

        var parts = new List<SurgeryPartInfo>();
        var limbCount = 0;

        foreach (var limb in _body.EnumerateOrgans<WoundableComponent>(body))
        {
            limbCount++;
            var surgeries = new List<SurgeryInfo>();

            foreach (var id in _surgeries)
            {
                if (!IsSurgeryValid(body, limb.Owner, id, out var surgery))
                    continue;

                var steps = new List<SurgeryStepInfo>();
                foreach (var stepId in surgery.Comp.Steps)
                {
                    if (GetSingleton(stepId) is not { } stepEnt || !TryComp<SurgeryStepComponent>(stepEnt, out var step))
                        continue;

                    string? toolName = null;
                    if (step.Tool != null)
                    {
                        foreach (var reg in step.Tool.Values)
                        {
                            toolName = (reg.Component as ISurgeryToolComponent)?.ToolName;
                            break;
                        }
                    }

                    steps.Add(new SurgeryStepInfo(stepId, _prototypes.Index<EntityPrototype>(stepId.Id).Name, toolName, IsStepComplete(body, limb.Owner, surgery, stepEnt)));
                }

                surgeries.Add(new SurgeryInfo(id, _prototypes.Index<EntityPrototype>(id.Id).Name, surgery.Comp.Priority, steps));
            }

            if (surgeries.Count == 0)
                continue;

            parts.Add(new SurgeryPartInfo(GetNetEntity(limb.Owner), Name(limb.Owner), GetOrder(limb.Comp1.Category?.Id), surgeries));
        }

        Log.Info($"Surgery: {limbCount} woundable limbs, sending {parts.Count} limbs with surgeries, {_surgeries.Count} surgeries known.");
        RaiseNetworkEvent(new SurgeryStateEvent(GetNetEntity(body), parts), actor.PlayerSession);
    }

    private static int GetOrder(string? category)
    {
        if (category == null)
            return 10;

        if (category.StartsWith("Head"))
            return 1;

        if (category.StartsWith("Torso") || category.StartsWith("Chest"))
            return 2;

        if (category.StartsWith("Arm"))
            return 4;

        if (category.StartsWith("Hand"))
            return 5;

        if (category.StartsWith("Leg"))
            return 6;

        if (category.StartsWith("Foot"))
            return 7;

        return 9;
    }

    #endregion
}
