// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 gus <august.eymann@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Goobstation.TableSlam;

/// <summary>
/// Marks an entity as recently slammed onto a table, making it vulnerable to
/// follow-up shoves (disarms) that can paralyze it.
/// </summary>
[RegisterComponent]
public sealed partial class PostTabledComponent : Component
{
    [DataField]
    public TimeSpan PostTabledShovableTime = TimeSpan.Zero;

    [DataField]
    public float ParalyzeChance = 0.35f;
}
