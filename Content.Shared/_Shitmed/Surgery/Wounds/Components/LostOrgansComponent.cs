// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - the organs that were taken out of a body. Only organs that were removed are listed, so a species that never had one is not affected.
using Content.Shared.Body;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Wounds.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class LostOrgansComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<OrganCategoryPrototype>> Categories = new();
}
