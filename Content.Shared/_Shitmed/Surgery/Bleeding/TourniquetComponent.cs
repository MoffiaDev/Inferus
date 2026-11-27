// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - limb tourniquets. Goob-Station applies a tourniquet to the whole body, here it goes on the limb the user targets.
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Shitmed.Surgery.Bleeding;

/// <summary>
/// An item that can be tied around an arm or a leg to stop the bleeding of that limb.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class TourniquetComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(1.5);

    /// <summary>
    /// A tourniquet hurts, this much pain is added when it is tied on.
    /// </summary>
    [DataField]
    public float Pain = 15f;
}

/// <summary>
/// Put on a limb organ while a tourniquet is tied around it.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TourniquetedComponent : Component
{
    /// <summary>
    /// The item that is given back when the tourniquet is taken off.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId? Item;
}

[Serializable, NetSerializable]
public sealed partial class TourniquetDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// The id of the organ category of the limb being tied.
    /// </summary>
    public string Limb = string.Empty;

    public override DoAfterEvent Clone() => this;
}
