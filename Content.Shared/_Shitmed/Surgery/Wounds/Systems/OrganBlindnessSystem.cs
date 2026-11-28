// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - a blind body with a viewcone has the cone close in over it, instead of the grey blind overlay.
using Content.Shared._Funkystation.Viewcone;
using Content.Shared.Eye.Blinding.Systems;
using Robust.Shared.Network;

namespace Content.Shared._Shitmed.Surgery.Wounds.Systems;

public sealed partial class OrganBlindnessSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;

    [SubscribeLocalEvent]
    private void OnBlindnessChanged(Entity<ViewconeBlindnessComponent> ent, ref BlindnessChangedEvent args)
    {
        if (_net.IsClient || ent.Comp.IsBlind == args.Blind)
            return;

        ent.Comp.IsBlind = args.Blind;
        Dirty(ent);
    }
}
