// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/SharedSurgerySystem.Steps.cs, organ steps) // Inferus
// Inferus - taking internal organs out of a limb and putting them back in.
using Content.Shared._Shitmed.Surgery.Operations;
using Content.Shared.Body;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server._Shitmed.Surgery;

public sealed partial class SurgerySystem
{
    /// <summary>
    /// Finds the internal organ of this category that sits directly in the limb.
    /// </summary>
    private bool TryGetInternalOrgan(EntityUid part, ProtoId<OrganCategoryPrototype> category, out EntityUid organ)
    {
        foreach (var child in _organRelation.AllChildren(part))
        {
            if (child.Comp.Parent != part
                || !HasComp<InternalChildOrganComponent>(child.Owner)
                || !TryComp<OrganComponent>(child.Owner, out var comp)
                || comp.Category != category)
                continue;

            organ = child.Owner;
            return true;
        }

        organ = default;
        return false;
    }

    #region Extraction

    [SubscribeLocalEvent]
    private void OnExtractOrganStep(Entity<SurgeryExtractOrganStepComponent> ent, ref SurgeryStepEvent args)
    {
        if (!TryGetInternalOrgan(args.Part, ent.Comp.Category, out var organ)
            || !TryComp<BodyComponent>(args.Body, out var body)
            || body.Organs is not { } container)
            return;

        _organRelation.Orphan(organ);

        if (!_container.Remove(organ, container))
            return;

        // If the hands are full, the organ stays on the table.
        _hands.TryPickupAnyHand(args.User, organ);
    }

    [SubscribeLocalEvent]
    private void OnExtractOrganCheck(Entity<SurgeryExtractOrganStepComponent> ent, ref SurgeryStepCompleteCheckEvent args)
    {
        if (TryGetInternalOrgan(args.Part, ent.Comp.Category, out _))
            args.Cancelled = true;
    }

    #endregion

    #region Implanting

    [SubscribeLocalEvent]
    private void OnImplantOrganStep(Entity<SurgeryImplantOrganStepComponent> ent, ref SurgeryStepEvent args)
    {
        if (TryGetInternalOrgan(args.Part, ent.Comp.Category, out _)
            || !TryComp<BodyComponent>(args.Body, out var body)
            || body.Organs is not { } container)
            return;

        var organ = args.Tool;
        if (!IsOrganFor(organ, ent.Comp.Category))
        {
            _popup.PopupEntity(Loc.GetString("surgery-implant-wrong-organ"), args.Body, args.User, PopupType.SmallCaution);
            return;
        }

        if (!_container.Insert(organ, container, force: true))
            return;

        _organRelation.Relate(args.Part, organ);
    }

    [SubscribeLocalEvent]
    private void OnImplantOrganCheck(Entity<SurgeryImplantOrganStepComponent> ent, ref SurgeryStepCompleteCheckEvent args)
    {
        if (!TryGetInternalOrgan(args.Part, ent.Comp.Category, out _))
            args.Cancelled = true;
    }

    #endregion

    [SubscribeLocalEvent]
    private void OnOrganCondition(Entity<SurgeryOrganConditionComponent> ent, ref SurgeryValidEvent args)
    {
        var present = TryGetInternalOrgan(args.Part, ent.Comp.Category, out _);

        if (ent.Comp.Present)
        {
            args.Cancelled |= !present;
            return;
        }

        var isHost = TryComp<OrganComponent>(args.Part, out var host) && host.Category == ent.Comp.Host;
        args.Cancelled |= present || !isHost;
    }
}
