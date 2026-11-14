// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Grab;
using Robust.Shared.GameStates;

namespace Content.Shared._Goobstation.Grab;

[RegisterComponent, NetworkedComponent]
public sealed partial class GrabModifierComponent : Component
{
    [DataField]
    public GrabStage StartingGrabStage = GrabStage.Soft;

    [DataField]
    public float GrabEscapeModifier;

    [DataField]
    public float GrabEscapeMultiplier = 1f;

    [DataField]
    public float GrabMoveSpeedMultiplier = 1f;
}
