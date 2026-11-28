// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (Content.Server/_Starlight/HoloItem/HoloItemComponent.cs) // Inferus
// Inferus - the component is shared so that the client knows it, the system is on the server.
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.HoloItem;

/// <summary>
/// An item that makes a temporary hologram of another item for a charge of its power cell.
/// </summary>
[RegisterComponent]
public sealed partial class HoloItemComponent : Component
{
    /// <summary>
    /// The item the hologram is made of.
    /// </summary>
    [DataField]
    public EntProtoId? ItemPrototype;

    /// <summary>
    /// If true the hologram is used on the target that was clicked, if false it is put down where it was clicked.
    /// </summary>
    [DataField]
    public bool UseOnTarget = true;

    [DataField]
    public float ChargeUse = 50f;

    /// <summary>
    /// The names of the components the target has to have.
    /// </summary>
    [DataField]
    public List<string> RequiredComponents = new();
}
