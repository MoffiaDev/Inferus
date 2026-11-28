// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (Content.Shared/_Starlight/Medical/Limbs) // Inferus
// Inferus - a cybernetic hand that keeps tools inside it. The action of the hand takes them out into their own hands of the body and puts them back.
using Content.Shared.Actions;
using Content.Shared.Body;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Organs;

/// <summary>
/// The tools a hand organ keeps, spawned when the organ is made.
/// </summary>
[RegisterComponent]
public sealed partial class LimbItemStorageComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId> Items = new();

    [ViewVariables]
    public List<EntityUid> ItemEntities = new();

    /// <summary>
    /// The container of the organ that holds the tools while they are put away.
    /// </summary>
    [DataField]
    public string ContainerId = "cyberlimb";
}

/// <summary>
/// Lets the body take the tools of <see cref="LimbItemStorageComponent"/> out and put them back, with an action it gets while it has the organ.
/// </summary>
[RegisterComponent]
public sealed partial class LimbItemDeployerComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Action;

    [ViewVariables]
    public EntityUid? ActionEntity;

    [DataField]
    public SoundSpecifier? Sound;

    [ViewVariables]
    public bool Toggled;

    /// <summary>
    /// What the extra hands the tools are held in accept.
    /// </summary>
    [DataField]
    public EntityWhitelist? HandWhitelist;
}

/// <summary>
/// Raised on the body by the action of a hand that has tools.
/// </summary>
public sealed partial class ToggleLimbEvent : InstantActionEvent
{
    /// <summary>
    /// The hand the action belongs to.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;
}
