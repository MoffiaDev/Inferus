// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Goobstation.Grab;

// Can't have inventory relays because it must be in common...
[ByRefEvent]
public record struct GrabAttemptEvent(
    EntityUid Puller,
    bool IgnoreCombatMode = false,
    GrabStage? GrabStageOverride = null,
    float EscapeAttemptModifier = 1f)
{
    public bool Grabbed = false;
}

[ByRefEvent]
public record struct GrabAttemptReleaseEvent(
    EntityUid? user,
    EntityUid puller)
{
    public bool Released = true;
}

[ByRefEvent]
public record struct CheckGrabbedEvent(bool IsGrabbed = false);

[ByRefEvent]
public record struct RaiseGrabModifierEventEvent(
    EntityUid User,
    int Stage,
    GrabStage? NewStage = null,
    float Multiplier = 1f,
    float Modifier = 0f,
    float SpeedMultiplier = 1f);

[ByRefEvent]
public record struct FindGrabbingItemEvent(EntityUid? Grabbed = null, EntityUid? GrabbingItem = null);

[ByRefEvent]
public readonly record struct StopGrabbingItemPullEvent(EntityUid PulledUid);
