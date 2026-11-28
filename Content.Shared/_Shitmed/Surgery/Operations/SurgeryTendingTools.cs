// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Tools) // Inferus
// Inferus - the tools of the tending steps that were not in Inferus yet. Tweezers have no step of their own.
using Robust.Shared.GameStates;

namespace Content.Shared._Shitmed.Surgery.Operations;

[RegisterComponent, NetworkedComponent]
public sealed partial class TweezersComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "tweezers";

    [DataField]
    public float Speed { get; set; } = 1f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class TendingComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "tending tool";

    [DataField]
    public float Speed { get; set; } = 1f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class StitchesComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "stitches";

    [DataField]
    public float Speed { get; set; } = 1f;
}
