// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.MartialArts;
using Content.Shared._Goobstation.MartialArts.Components;

namespace Content.Client._Goobstation.MartialArts;

public sealed partial class MartialArtsSystem : SharedMartialArtsSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CanPerformComboComponent, GetPerformedAttackTypesEvent>(OnGetAttackTypes);
    }

    private void OnGetAttackTypes(Entity<CanPerformComboComponent> ent, ref GetPerformedAttackTypesEvent args)
    {
        args.AttackTypes = ent.Comp.LastAttacks;
    }
}
