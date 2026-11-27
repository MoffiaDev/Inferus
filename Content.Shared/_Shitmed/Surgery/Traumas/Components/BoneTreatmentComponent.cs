// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - bone treatment. Cracked and broken bones never heal by themselves, this is how they get fixed.
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Shitmed.Surgery.Traumas;

/// <summary>
/// An item that mends the bone of the limb the user targets. Used up on success.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BoneTreatmentComponent : Component
{
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(4);

    /// <summary>
    /// How much bone integrity is given back. A broken bone ends up damaged, and heals on from there by itself.
    /// </summary>
    [DataField]
    public float Integrity = 30f;

    /// <summary>
    /// Setting a bone hurts.
    /// </summary>
    [DataField]
    public float Pain = 10f;

    /// <summary>
    /// The bone has to be at least this bad for the treatment to be worth it.
    /// </summary>
    [DataField]
    public BoneSeverity MinimumSeverity = BoneSeverity.Cracked;
}

[Serializable, NetSerializable]
public sealed partial class BoneTreatmentDoAfterEvent : DoAfterEvent
{
    /// <summary>
    /// The id of the organ category of the limb being treated.
    /// </summary>
    public string Limb = string.Empty;

    public override DoAfterEvent Clone() => this;
}
