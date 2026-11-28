// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/SurgeryEvents.cs, SurgeryStepEvent.cs, SurgeryUI.cs) // Inferus
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Shitmed.Surgery.Operations;

/// <summary>
/// Raised on a surgery or step prototype entity to ask whether it can be done on this limb. Conditions set Cancelled.
/// </summary>
[ByRefEvent]
public record struct SurgeryValidEvent(EntityUid Body, EntityUid Part)
{
    public bool Cancelled;
}

/// <summary>
/// Raised on a step prototype entity to ask whether it is already done. Anything that is not done sets Cancelled.
/// </summary>
[ByRefEvent]
public record struct SurgeryStepCompleteCheckEvent(EntityUid Body, EntityUid Part, EntityUid Surgery)
{
    public bool Cancelled;
}

/// <summary>
/// Raised on a step prototype entity when the step has been done. Extra effects (organs, bones, trauma) subscribe to this.
/// </summary>
[ByRefEvent]
public record struct SurgeryStepEvent(EntityUid User, EntityUid Body, EntityUid Part, EntityUid Tool, EntityUid Surgery, EntityUid Step);

[Serializable, NetSerializable]
public sealed partial class SurgeryDoAfterEvent : SimpleDoAfterEvent
{
    public readonly EntProtoId Surgery;
    public readonly EntProtoId Step;
    public readonly NetEntity Part;

    public SurgeryDoAfterEvent(EntProtoId surgery, EntProtoId step, NetEntity part)
    {
        Surgery = surgery;
        Step = step;
        Part = part;
    }
}

#region UI

[Serializable, NetSerializable]
public sealed class SurgeryStepInfo(EntProtoId id, string name, string? tool, bool complete)
{
    public readonly EntProtoId Id = id;
    public readonly string Name = name;
    public readonly string? Tool = tool;
    public readonly bool Complete = complete;
}

[Serializable, NetSerializable]
public sealed class SurgeryInfo(EntProtoId id, string name, int priority, List<SurgeryStepInfo> steps)
{
    public readonly EntProtoId Id = id;
    public readonly string Name = name;
    public readonly int Priority = priority;
    public readonly List<SurgeryStepInfo> Steps = steps;
}

[Serializable, NetSerializable]
public sealed class SurgeryPartInfo(NetEntity part, string name, int order, List<SurgeryInfo> surgeries)
{
    public readonly NetEntity Part = part;
    public readonly string Name = name;
    public readonly int Order = order;
    public readonly List<SurgeryInfo> Surgeries = surgeries;
}

/// <summary>
/// Sent by the server to the surgeon: open the surgery window for this body, or refresh it.
/// </summary>
[Serializable, NetSerializable]
public sealed class SurgeryStateEvent(NetEntity body, List<SurgeryPartInfo> parts) : EntityEventArgs
{
    public readonly NetEntity Body = body;
    public readonly List<SurgeryPartInfo> Parts = parts;
}

/// <summary>
/// Sent by the surgeon's client when a step button is pressed.
/// </summary>
[Serializable, NetSerializable]
public sealed class SurgeryStepChosenEvent(NetEntity body, NetEntity part, EntProtoId surgery, EntProtoId step) : EntityEventArgs
{
    public readonly NetEntity Body = body;
    public readonly NetEntity Part = part;
    public readonly EntProtoId Surgery = surgery;
    public readonly EntProtoId Step = step;
}

#endregion
