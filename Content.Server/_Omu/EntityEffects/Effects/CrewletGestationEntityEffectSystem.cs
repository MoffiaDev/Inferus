// SPDX-License-Identifier: MIT

using Content.Server._Omu.Crewlet;
using Content.Shared._Omu.EntityEffects.Effects;
using Content.Shared.EntityEffects;
using Content.Shared.Humanoid;

namespace Content.Server._Omu.EntityEffects.Effects;

public sealed partial class CrewletGestationEntityEffectSystem
    : EntityEffectSystem<HumanoidProfileComponent, CrewletGestationEntityEffect>
{
    [Dependency] private CrewletGestationSystem _gestation = default!; // Inferus

    protected override void Effect(Entity<HumanoidProfileComponent> entity,
        ref EntityEffectEvent<CrewletGestationEntityEffect> args)
    {
        _gestation.Trigger(entity, args.Effect);
    }
}
