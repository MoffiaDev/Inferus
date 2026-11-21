using System;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;

namespace Content.Shared._CD.Records;

/// <summary>
/// Helper utilities for deriving record metadata from a character profile.
/// </summary>
public static class CharacterRecordSizeHelper
{
    private const float FallbackWeight = 71f; // Inferus: matches the lobby weight label's fallback

    public static bool TryCalculateMetrics(
        HumanoidCharacterProfile? profile,
        IPrototypeManager prototypes,
        out int heightCentimeters,
        out int weightKilograms)
    {
        heightCentimeters = 0;
        weightKilograms = 0;

        if (profile == null)
            return false;

        // Species could have been removed or renamed; guard against missing prototypes.
        if (!prototypes.TryIndex<SpeciesPrototype>(profile.Species, out var species))
            return false;

        // Inferus - Inferus uses the Goobstation height/width sliders, which store the scales on the profile
        // and the average size on the species, instead of Starlight's StandardSize/StandardWeight/StandardDensity.
        var heightScale = profile.Height;
        var widthScale = profile.Width;

        var height = species.AverageHeight * heightScale;
        // Same formula as the lobby weight label (Goobstation HumanoidProfileEditor.UpdateWeight), so both agree.
        var weight = FallbackWeight;
        var factory = IoCManager.Resolve<IComponentFactory>();
        if (prototypes.TryIndex(species.Prototype, out var proto)
            && proto.TryGetComponent<FixturesComponent>(out var fixture, factory)
            && fixture.Fixtures.TryGetValue("fix1", out var fix1))
        {
            var avg = (widthScale + heightScale) / 2;
            weight = MathF.Round(MathF.PI * MathF.Pow(fix1.Shape.Radius * avg, 2) * fix1.Density);
        }
        // End Inferus

        // Clamp to configured profile limits so downstream DB/UI consumers cannot explode.
        heightCentimeters = Math.Clamp((int) Math.Round(height), 0, PlayerProvidedCharacterRecords.MaxHeight);
        weightKilograms = Math.Clamp((int) Math.Round(weight), 0, PlayerProvidedCharacterRecords.MaxWeight);
        return true;
    }

    public static PlayerProvidedCharacterRecords WithCalculatedMetrics(
        PlayerProvidedCharacterRecords records,
        HumanoidCharacterProfile? profile,
        IPrototypeManager prototypes)
    {
        // Only mutate the record when we successfully derived fresh numbers.
        return TryCalculateMetrics(profile, prototypes, out var heightCm, out var weightKg)
            ? records.WithHeight(heightCm).WithWeight(weightKg)
            : records;
    }
}
