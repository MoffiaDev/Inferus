// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Tools) // Inferus
using Robust.Shared.GameStates;

namespace Content.Shared._Shitmed.Surgery.Operations;

public interface ISurgeryToolComponent
{
    /// <summary>
    /// What the tool is called in "you need a ..." messages.
    /// </summary>
    string ToolName { get; }

    /// <summary>
    /// A step takes its duration divided by this.
    /// </summary>
    float Speed { get; }
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ScalpelComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "scalpel";

    [DataField]
    public float Speed { get; set; } = 1f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class RetractorComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "retractor";

    [DataField]
    public float Speed { get; set; } = 1f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class HemostatComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "hemostat";

    [DataField]
    public float Speed { get; set; } = 1f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class CauteryComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "cautery";

    [DataField]
    public float Speed { get; set; } = 1f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class DrillComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "drill";

    [DataField]
    public float Speed { get; set; } = 1f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class BoneSawComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "bone saw";

    [DataField]
    public float Speed { get; set; } = 1f;
}
