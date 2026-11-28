// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - the components limb surgery runs on: markers on the limb, the tools for bones, and the severed limb that counts as a tool.
using Robust.Shared.GameStates;

namespace Content.Shared._Shitmed.Surgery.Operations;

/// <summary>
/// The bone of the limb has been sawn through.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BoneSawedComponent : Component;

/// <summary>
/// The bone of the limb has been set, the gel is still to come. Used by the mend a bone surgery.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BoneSetComponent : Component;

/// <summary>
/// Put on the part a severed limb was just attached to, until the surgery is finished.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LimbReattachedComponent : Component
{
    /// <summary>
    /// The limb that was attached. Only the server uses this.
    /// </summary>
    [ViewVariables]
    public EntityUid? Limb;
}

/// <summary>
/// The bone of a freshly attached limb has been set.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class LimbBoneSetComponent : Component;

/// <summary>
/// Put on the detached body that an amputation leaves lying around. It counts as the tool of the attach step, and remembers
/// the limb and the organ the limb was attached to.
/// </summary>
[RegisterComponent]
public sealed partial class DetachedLimbComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "severed limb";

    public float Speed => 1f;

    [ViewVariables]
    public EntityUid? Limb;

    [ViewVariables]
    public EntityUid? Parent;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class BoneSetterComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "bone setter";

    [DataField]
    public float Speed { get; set; } = 1f;
}

[RegisterComponent, NetworkedComponent]
public sealed partial class BoneGelComponent : Component, ISurgeryToolComponent
{
    public string ToolName => "bone gel";

    [DataField]
    public float Speed { get; set; } = 1f;
}
