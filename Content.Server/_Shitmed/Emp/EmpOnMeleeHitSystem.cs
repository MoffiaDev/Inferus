// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (Content.Server/Emp/EmpSystem.cs, HandleMeleeHitTrigger) // Inferus
using Content.Shared._Shitmed.Emp;
using Content.Shared.Emp;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Server._Shitmed.Emp;

public sealed partial class EmpOnMeleeHitSystem : EntitySystem
{
    [Dependency] private SharedEmpSystem _emp = default!;

    [SubscribeLocalEvent]
    private void OnMeleeHit(Entity<EmpOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.IsHit || TryComp<ItemToggleComponent>(ent, out var toggle) && !toggle.Activated)
            return;

        foreach (var hit in args.HitEntities)
        {
            if (TerminatingOrDeleted(hit))
                continue;

            _emp.EmpPulse(Transform(hit).Coordinates, ent.Comp.Range, ent.Comp.EnergyConsumption, ent.Comp.DisableDuration, args.User, predicted: false);
        }
    }
}
