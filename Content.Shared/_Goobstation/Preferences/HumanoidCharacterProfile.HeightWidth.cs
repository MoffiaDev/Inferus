// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 MarkerWicker <markerWicker@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Preferences;

// Goobstation - port EE height/width sliders
// Inferus - moved from HumanoidCharacterProfile.cs into a partial to keep upstream edits small
public sealed partial class HumanoidCharacterProfile
{
    /// <summary>
    ///     Height scale of the character, clamped to the species' MinHeight and MaxHeight.
    /// </summary>
    [DataField]
    public float Height { get; set; } = 1f;

    /// <summary>
    ///     Width scale of the character, clamped to the species' MinWidth and MaxWidth.
    /// </summary>
    [DataField]
    public float Width { get; set; } = 1f;

    public HumanoidCharacterProfile WithHeight(float height)
    {
        return new(this) { Height = height };
    }

    public HumanoidCharacterProfile WithWidth(float width)
    {
        return new(this) { Width = width };
    }

    /// <summary>
    ///     Picks a random height and width within the species' limits.
    /// </summary>
    public static (float Height, float Width) RandomHeightWidth(SpeciesPrototype species)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        return (random.NextFloat(species.MinHeight, species.MaxHeight),
            random.NextFloat(species.MinWidth, species.MaxWidth));
    }

    private void EnsureValidHeightWidth(SpeciesPrototype species)
    {
        // Non-positive or NaN values come from old or broken profiles, use species defaults.
        if (!(Height > 0f))
            Height = species.DefaultHeight;
        if (!(Width > 0f))
            Width = species.DefaultWidth;

        Height = Math.Clamp(Height, species.MinHeight, species.MaxHeight);
        Width = Math.Clamp(Width, species.MinWidth, species.MaxWidth);
    }
}
