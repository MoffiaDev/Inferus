// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

// Goobstation - Martial Arts, ported from Goob-Station PR #1868
using Content.Shared._Goobstation.Grab;

namespace Content.Shared.Movement.Pulling.Events;

public sealed class CheckGrabOverridesEvent : EntityEventArgs
{
    public CheckGrabOverridesEvent(GrabStage stage)
    {
        Stage = stage;
    }

    public GrabStage Stage { get; set; }
}
