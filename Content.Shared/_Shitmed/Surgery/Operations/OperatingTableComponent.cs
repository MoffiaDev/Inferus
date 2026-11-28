// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Tools/OperatingTableComponent.cs) // Inferus
// Inferus - a bed that surgery goes better on. It only counts for a patient who is buckled to it.
namespace Content.Shared._Shitmed.Surgery.Operations;

[RegisterComponent]
public sealed partial class OperatingTableComponent : Component
{
    /// <summary>
    /// Steps take this much less time, 1.25 means a step is done in four fifths of the time.
    /// </summary>
    [DataField]
    public float SpeedMultiplier = 1.25f;

    /// <summary>
    /// What an unsterile step poisons the patient with is multiplied by this, the table is kept clean.
    /// </summary>
    [DataField]
    public float ToxinMultiplier = 0.5f;
}
