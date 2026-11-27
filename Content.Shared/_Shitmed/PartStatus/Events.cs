// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/PartStatus/Events.cs) // Inferus
using Robust.Shared.Serialization;

namespace Content.Shared._Shitmed.PartStatus;

/// <summary>
/// Sent by a client that clicked the part status doll, asking for a written report of its own limbs.
/// </summary>
[Serializable, NetSerializable]
public sealed class GetPartStatusEvent(NetEntity uid) : EntityEventArgs
{
    public readonly NetEntity Uid = uid;
}
