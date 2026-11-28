// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (Content.Shared/_Starlight/Storage/StrippingInternalStorageButtonPressed.cs and PrivateStorageDoAfterEvent.cs) // Inferus
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Shitmed.Surgery.Organs;

/// <summary>
/// Sent from the strip menu when the button of a storage organ, like a cargo leg, is pressed.
/// </summary>
[Serializable, NetSerializable]
public sealed class StrippingOrganStorageButtonPressed : BoundUserInterfaceMessage
{
    public readonly NetEntity Organ;

    public StrippingOrganStorageButtonPressed(NetEntity organ)
    {
        Organ = organ;
    }
}

/// <summary>
/// Finished when the one who strips has waited long enough to open the storage organ of someone else.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class OrganStorageDoAfterEvent : SimpleDoAfterEvent;
