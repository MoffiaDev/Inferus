// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Grab;
using Content.Shared.Inventory;

namespace Content.Shared._Goobstation.Grab;

[ByRefEvent]
public record struct GrabModifierEvent(EntityUid User, GrabStage Stage) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.GLOVES;

    public GrabStage? NewStage = null;

    public float Multiplier = 1f;

    public float Modifier = 0f;

    public float SpeedMultiplier = 1f;
}
