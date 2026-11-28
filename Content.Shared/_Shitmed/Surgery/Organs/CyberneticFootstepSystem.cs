// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (the FootstepModifier of the cybernetic leg parts) // Inferus
// Inferus - the mover controller asks the body for a footstep sound. Leg organs with a FootstepModifier answer, unless an EMP has them off.
using Content.Shared.Body;
using Content.Shared.Emp;
using Content.Shared.Movement.Components;

namespace Content.Shared._Shitmed.Surgery.Organs;

public sealed partial class CyberneticFootstepSystem : EntitySystem
{
    [Dependency] private BodySystem _body = default!;

    [SubscribeLocalEvent]
    private void OnGetSound(Entity<BodyComponent> ent, ref GetCyberneticFootstepSoundEvent args)
    {
        foreach (var organ in _body.EnumerateOrgans<FootstepModifierComponent>(ent.Owner))
        {
            if (organ.Comp2.FootstepSoundCollection is not { } sound || HasComp<EmpDisabledComponent>(organ.Owner))
                continue;

            args.Sound = sound;
            return;
        }
    }
}
