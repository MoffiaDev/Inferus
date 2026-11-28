// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/DoAfter/GetDoAfterDelayMultiplierEvent.cs) // Inferus
namespace Content.Shared._Shitmed.DoAfter;

/// <summary>
/// Raised on the user of a do-after when it starts, systems multiply <see cref="Multiplier"/> to make it slower or faster.
/// </summary>
public sealed class GetDoAfterDelayMultiplierEvent : EntityEventArgs
{
    public float Multiplier = 1f;
}