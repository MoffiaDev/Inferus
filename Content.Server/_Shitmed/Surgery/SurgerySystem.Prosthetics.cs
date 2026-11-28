// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - which severed limbs a part accepts. The limb that was cut off it always fits, a prosthetic fits any empty slot of its kind.
using Content.Shared._Shitmed.Surgery.Operations;
using Content.Shared._Shitmed.Surgery.Organs;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;

namespace Content.Server._Shitmed.Surgery;

public sealed partial class SurgerySystem
{
    private bool CanAttachLimb(EntityUid tool, DetachedLimbComponent detached, EntityUid limb, EntityUid body, EntityUid part)
    {
        if (detached.Parent == part)
            return true;

        if (!TryComp<ProstheticLimbComponent>(tool, out var prosthetic)
            || !TryComp<OrganComponent>(part, out var partOrgan)
            || partOrgan.Category != prosthetic.AttachCategory
            || !TryComp<OrganComponent>(limb, out var limbOrgan))
            return false;

        // The slot has to be empty, a prosthetic does not replace a limb that is still there.
        foreach (var existing in _body.EnumerateOrgans<WoundableComponent>(body))
        {
            if (existing.Comp1.Category == limbOrgan.Category)
                return false;
        }

        return true;
    }
}
