// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (the FootstepModifier of the cybernetic leg parts) // Inferus
// Inferus - the mover controller asks the body for a footstep sound. The steps go left, right, left, and so on, and each leg
// answers with its own FootstepModifier unless an EMP has it off. A step of a leg without one is left to the shoes and the floor.
using Content.Shared.Body;
using Content.Shared.Emp;
using Content.Shared.Movement.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Organs;

public sealed partial class CyberneticFootstepSystem : EntitySystem
{
    private static readonly ProtoId<OrganCategoryPrototype> LegLeft = "LegLeft";
    private static readonly ProtoId<OrganCategoryPrototype> LegRight = "LegRight";

    [Dependency] private BodySystem _body = default!;

    [SubscribeLocalEvent]
    private void OnGetSound(Entity<BodyComponent> ent, ref GetCyberneticFootstepSoundEvent args)
    {
        var state = EnsureComp<CyberneticStepComponent>(ent);

        // Every step the foot changes
        var side = state.NextRight ? LegRight : LegLeft;
        state.NextRight = !state.NextRight;

        foreach (var organ in _body.EnumerateOrgans<FootstepModifierComponent>(ent.Owner))
        {
            if (organ.Comp1.Category != side
                || organ.Comp2.FootstepSoundCollection is not { } sound
                || HasComp<EmpDisabledComponent>(organ.Owner))
                continue;

            args.Sound = sound;
            return;
        }
    }
}
