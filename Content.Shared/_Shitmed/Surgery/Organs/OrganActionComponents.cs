// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (LimbWithAction, StorageOrgan, JumpActionEvent) // Inferus
// Inferus - actions that an organ gives its body, for the pilebunker leg (jump) and the cargo leg (storage).
using Content.Shared.Actions;
using Content.Shared.Body;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Organs;

/// <summary>
/// The body gets this action while it has the organ.
/// </summary>
[RegisterComponent]
public sealed partial class OrganActionComponent : Component
{
    [DataField(required: true)]
    public EntProtoId Action;

    [ViewVariables]
    public EntityUid? ActionEntity;
}

/// <summary>
/// Lets the body use <see cref="OrganJumpEvent"/> while the organ is in it and not switched off by an EMP.
/// </summary>
[RegisterComponent]
public sealed partial class OrganJumpComponent : Component;

/// <summary>
/// The organ is a storage that its own body can open with <see cref="OpenOrganStorageEvent"/>. Other people cannot reach it.
/// </summary>
[RegisterComponent]
public sealed partial class OrganStorageComponent : Component;

/// <summary>
/// A short throw of the body towards a point. The jump of a pilebunker leg.
/// </summary>
public sealed partial class OrganJumpEvent : WorldTargetActionEvent
{
    [DataField]
    public float Distance = 5f;

    /// <summary>
    /// If true the body jumps to the pointer when that is closer than the distance, if false it always jumps the full distance.
    /// </summary>
    [DataField]
    public bool ToPointer = true;

    [DataField]
    public float Speed = 15f;

    [DataField]
    public SoundSpecifier? Sound;
}

/// <summary>
/// Opens the storage of the organ with this category.
/// </summary>
public sealed partial class OpenOrganStorageEvent : InstantActionEvent
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;
}
