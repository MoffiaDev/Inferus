// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - the effects and conditions of the limb, bone and wound surgeries, and the sterile gear check.
using System.Linq;
using Content.Shared._Shitmed.Surgery.Operations;
using Content.Shared._Shitmed.Surgery.Traumas;
using Content.Shared._Shitmed.Surgery.Traumas.Systems;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Surgery.Wounds.Systems;
using Content.Shared.Body;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Shitmed.Surgery;

public sealed partial class SurgerySystem
{
    /// <summary>
    /// The masks and gloves that count as sterile. Everyone within <see cref="SterileRange"/> of the patient has to wear one of each.
    /// </summary>
    private static readonly HashSet<string> SterileMasks = new() { "ClothingMaskSterile" };

    private static readonly HashSet<string> SterileGloves = new() { "ClothingHandsGlovesLatex", "ClothingHandsGlovesNitrile" };

    private const float SterileRange = 3f;
    private const float LimbRange = 3f;

    [Dependency] private WoundSystem _wounds = default!;
    [Dependency] private BoneSystem _bone = default!;
    [Dependency] private OrganRelationSystem _organRelation = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private InventorySystem _inventory = default!;

    private bool IsSurgeryComplete(EntityUid body, EntityUid part, EntProtoId id)
    {
        if (GetSingleton(id) is not { } surgery || !TryComp<SurgeryComponent>(surgery, out var comp))
            return false;

        foreach (var stepId in comp.Steps)
        {
            if (GetSingleton(stepId) is not { } step || !IsStepComplete(body, part, surgery, step))
                return false;
        }

        return true;
    }

    #region Sterile

    private bool HasPpe(EntityUid uid)
    {
        return WearsOneOf(uid, "mask", SterileMasks) && WearsOneOf(uid, "gloves", SterileGloves);
    }

    private bool WearsOneOf(EntityUid uid, string slot, HashSet<string> allowed)
    {
        return _inventory.TryGetSlotEntity(uid, slot, out var item)
            && MetaData(item.Value).EntityPrototype?.ID is { } id
            && allowed.Contains(id);
    }

    #endregion

    #region Amputation

    [SubscribeLocalEvent]
    private void OnAmputateStep(Entity<SurgeryAmputateStepComponent> ent, ref SurgeryStepEvent args)
    {
        _wounds.TryAmputate(args.Part, force: true); // Inferus - the condition already keeps the torso out
    }

    [SubscribeLocalEvent]
    private void OnAmputateCheck(Entity<SurgeryAmputateStepComponent> ent, ref SurgeryStepCompleteCheckEvent args)
    {
        if (TryComp<OrganComponent>(args.Part, out var organ) && organ.Body == args.Body)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnAmputableCondition(Entity<SurgeryAmputableConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (!HasComp<DetachableOrganComponent>(args.Part)
            || !CanAmputateSurgically(args.Part) // Inferus
            || !TryComp<OrganComponent>(args.Part, out var organ)
            || organ.Body != args.Body)
            args.Cancelled = true;
    }

    #endregion

    #region Reattachment

    [SubscribeLocalEvent]
    private void OnAttachLimbStep(Entity<SurgeryAttachLimbStepComponent> ent, ref SurgeryStepEvent args)
    {
        if (!TryComp<DetachedLimbComponent>(args.Tool, out var detached)
            || detached.Limb is not { } limb
            || Deleted(limb)
            || !TryComp<BodyComponent>(args.Tool, out var oldBody)
            || oldBody.Organs is not { } source
            || !TryComp<BodyComponent>(args.Body, out var body)
            || body.Organs is not { } target)
        {
            RemComp<LimbReattachedComponent>(args.Part);
            return;
        }

        if (detached.Parent != args.Part)
        {
            RemComp<LimbReattachedComponent>(args.Part);
            _popup.PopupEntity(Loc.GetString("surgery-attach-wrong-limb"), args.Body, args.User, PopupType.SmallCaution);
            return;
        }

        // The limb and everything below it moves over, the relations between those organs stay as they were.
        var organs = source.ContainedEntities.ToList();
        foreach (var organ in organs)
        {
            _container.Insert(organ, target, force: true);
        }

        _organRelation.Relate(args.Part, limb);

        foreach (var organ in organs)
        {
            _wounds.OnLimbReattached(organ, args.Body);
        }

        _wounds.RefreshBodyDamage(args.Body);

        var marker = EnsureComp<LimbReattachedComponent>(args.Part);
        marker.Limb = limb;

        QueueDel(args.Tool);
    }

    [SubscribeLocalEvent]
    private void OnFinishReattachStep(Entity<SurgeryFinishReattachStepComponent> ent, ref SurgeryStepEvent args)
    {
        if (TryComp<LimbReattachedComponent>(args.Part, out var marker)
            && marker.Limb is { } limb
            && !Deleted(limb)
            && TryComp<BoneComponent>(limb, out var bone))
        {
            _bone.SetIntegrity((limb, bone), bone.IntegrityCap);
        }

        RemComp<LimbReattachedComponent>(args.Part);
        RemComp<LimbBoneSetComponent>(args.Part);
    }

    [SubscribeLocalEvent]
    private void OnFinishReattachCheck(Entity<SurgeryFinishReattachStepComponent> ent, ref SurgeryStepCompleteCheckEvent args)
    {
        if (HasComp<LimbReattachedComponent>(args.Part))
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnReattachCondition(Entity<SurgeryReattachConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (HasComp<LimbReattachedComponent>(args.Part))
            return;

        var bodyCoordinates = Transform(args.Body).Coordinates;
        var query = EntityQueryEnumerator<DetachedLimbComponent, TransformComponent>();
        while (query.MoveNext(out _, out var detached, out var xform))
        {
            if (detached.Parent == args.Part && _transform.InRange(xform.Coordinates, bodyCoordinates, LimbRange))
                return;
        }

        args.Cancelled = true;
    }

    #endregion

    #region Bones

    [SubscribeLocalEvent]
    private void OnBoneStep(Entity<SurgeryBoneStepComponent> ent, ref SurgeryStepEvent args)
    {
        if (!TryComp<BoneComponent>(args.Part, out var bone))
            return;

        var target = ent.Comp.Restore ? bone.IntegrityCap : (ent.Comp.Set ?? bone.BoneIntegrity);
        _bone.SetIntegrity((args.Part, bone), target);
    }

    [SubscribeLocalEvent]
    private void OnBoneCondition(Entity<SurgeryBoneConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (HasComp<BoneSetComponent>(args.Part))
            return;

        if (!TryComp<BoneComponent>(args.Part, out var bone) || bone.BoneSeverity < ent.Comp.Minimum)
            args.Cancelled = true;
    }

    #endregion

    #region Wounds

    [SubscribeLocalEvent]
    private void OnTendWoundsStep(Entity<SurgeryTendWoundsStepComponent> ent, ref SurgeryStepEvent args)
    {
        _wounds.TryHealLimb(args.Part, ent.Comp.Types, ent.Comp.Amount);
    }

    [SubscribeLocalEvent]
    private void OnTendWoundsCheck(Entity<SurgeryTendWoundsStepComponent> ent, ref SurgeryStepCompleteCheckEvent args)
    {
        if (_wounds.GetLimbWounds(args.Part, ent.Comp.Types) > 0)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnWoundedCondition(Entity<SurgeryWoundedConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (_wounds.GetLimbWounds(args.Part, ent.Comp.Types) <= 0)
            args.Cancelled = true;
    }

    #endregion
}
