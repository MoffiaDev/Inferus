// SPDX-License-Identifier: AGPL-3.0-or-later
using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared._Shitmed.Surgery.Wounds.Components;

/// <summary>
/// Marks a body whose damage is held by wounds on its limbs instead of by <c>InjurableComponent</c>.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WoundDamageModelComponent : Component
{
    /// <summary>
    /// Damage that no wound holds: damage types without a wound prototype, and overflow once the limbs have no integrity left.
    /// It is what keeps a body killable after every limb is used up.
    /// </summary>
    [DataField]
    public DamageSpecifier BodyDamage = new();
}
