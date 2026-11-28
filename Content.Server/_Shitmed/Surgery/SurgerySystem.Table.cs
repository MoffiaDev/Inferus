// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - what the operating table the patient lies on does for the surgery.
using Content.Shared._Shitmed.Surgery.Operations;
using Content.Shared.Buckle.Components;

namespace Content.Server._Shitmed.Surgery;

public sealed partial class SurgerySystem
{
    private OperatingTableComponent? GetTable(EntityUid body)
    {
        return TryComp<BuckleComponent>(body, out var buckle)
            && buckle.BuckledTo is { } strap
            && TryComp<OperatingTableComponent>(strap, out var table)
            ? table
            : null;
    }

    /// <summary>
    /// How much faster the steps go for this patient.
    /// </summary>
    private float GetTableSpeed(EntityUid body)
    {
        return Math.Max(0.1f, GetTable(body)?.SpeedMultiplier ?? 1f);
    }

    /// <summary>
    /// What an unsterile step poisons this patient with is multiplied by this.
    /// </summary>
    private float GetTableToxin(EntityUid body)
    {
        return GetTable(body)?.ToxinMultiplier ?? 1f;
    }
}
