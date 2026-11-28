// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - checks done before a step starts, and who can be amputated by surgery.
using Content.Shared._Shitmed.Surgery.Operations;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Server._Shitmed.Surgery;

public sealed partial class SurgerySystem
{
    private static readonly ProtoId<OrganCategoryPrototype> HeadCategory = "Head";

    /// <summary>
    /// Whether the held entity is an internal organ of this category that is free to be put into a body.
    /// </summary>
    private bool IsOrganFor(EntityUid organ, ProtoId<OrganCategoryPrototype> category)
    {
        return TryComp<OrganComponent>(organ, out var organComp)
            && organComp.Category == category
            && HasComp<InternalChildOrganComponent>(organ)
            && TryComp<ChildOrganComponent>(organ, out var child)
            && child.Parent == null;
    }

    [SubscribeLocalEvent]
    private void OnImplantOrganPreCheck(Entity<SurgeryImplantOrganStepComponent> ent, ref SurgeryStepPreCheckEvent args)
    {
        if (IsOrganFor(args.Tool, ent.Comp.Category))
            return;

        _popup.PopupEntity(Loc.GetString("surgery-implant-wrong-organ"), args.Body, args.User, PopupType.SmallCaution);
        args.Cancelled = true;
    }

    /// <summary>
    /// Limbs that cannot be torn off by damage can still be cut off by a surgeon, except the torso.
    /// </summary>
    private bool CanAmputateSurgically(EntityUid part)
    {
        if (!TryComp<WoundableComponent>(part, out var woundable))
            return false;

        return woundable.CanRemove
            || (TryComp<OrganComponent>(part, out var organ) && organ.Category == HeadCategory);
    }
}
