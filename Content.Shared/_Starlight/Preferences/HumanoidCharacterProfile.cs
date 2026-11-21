// SPDX-FileCopyrightText: 2025 Starlight
// SPDX-License-Identifier: Starlight-MIT

using System.Text.RegularExpressions;
using Content.Shared._Starlight.CCVar;
using Content.Shared.CCVar;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

// ReSharper disable once CheckNamespace
namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    /// <summary>
    /// Inferus - matches the max length of the sl_character_info database columns.
    /// </summary>
    public const int MaxCharacterInfoLength = 4096;

    /// <summary>
    /// Inferus - matches the max length of the star_light_profile.custom_specie_name database column.
    /// </summary>
    public const int MaxCustomSpecieNameLength = 32;

    private static readonly Regex RestrictedCustomSpecieNameRegex = new(@"[^A-Za-z0-9 '\-,]|\B\s+|\s+\B"); //Starlight

    /// <summary>
    /// A custom species name shown instead of the species name. Empty means the normal species name is used.
    /// </summary>
    [DataField] public string CustomSpecieName { get; set; } = "";

    [DataField] public string PersonalityDescription { get; set; } = string.Empty;

    [DataField] public string PersonalNotes { get; set; } = string.Empty;

    [DataField] public string OOCNotes { get; set; } = string.Empty;

    /// <summary>
    /// The outer physical description of the character.
    /// </summary>
    /// <remarks>
    /// Inferus - Starlight replaced FlavorText with PhysicalDescription. Inferus keeps the upstream
    /// <see cref="FlavorText"/> field as the storage for it, so existing flavor text carries over.
    /// </remarks>
    public string PhysicalDescription
    {
        get => FlavorText;
        set => FlavorText = value;
    }

    public HumanoidCharacterProfile WithPhysicalDesc(string physicalDesc)
    {
        return new(this) { PhysicalDescription = physicalDesc };
    }

    public HumanoidCharacterProfile WithPersonalityDesc(string personalityDesc)
    {
        return new(this) { PersonalityDescription = personalityDesc };
    }

    public HumanoidCharacterProfile WithPersonalNotes(string personalNotes)
    {
        return new(this) { PersonalNotes = personalNotes };
    }

    public HumanoidCharacterProfile WithOOCNotes(string oocNotes)
    {
        return new(this) { OOCNotes = oocNotes };
    }

    public HumanoidCharacterProfile WithCustomSpecieName(string customspeciename)
    {
        return new(this) { CustomSpecieName = customspeciename };
    }

    // Inferus - helpers so the upstream profile file only needs one-line hooks.
    private void CopyStarlightCharacterInfo(HumanoidCharacterProfile other)
    {
        PersonalityDescription = other.PersonalityDescription;
        PersonalNotes = other.PersonalNotes;
        OOCNotes = other.OOCNotes;
        CustomSpecieName = other.CustomSpecieName;
    }

    private bool StarlightCharacterInfoEquals(HumanoidCharacterProfile other)
    {
        return PersonalityDescription == other.PersonalityDescription
               && PersonalNotes == other.PersonalNotes
               && OOCNotes == other.OOCNotes
               && CustomSpecieName == other.CustomSpecieName;
    }

    private void EnsureValidStarlightCharacterInfo(IConfigurationManager configManager, IPrototypeManager prototypeManager)
    {
        CustomSpecieName = SanitizeCustomSpecieName(CustomSpecieName, configManager, prototypeManager);

        // The personality description shares the flavor text length limit, like the physical description.
        PersonalityDescription = SanitizeCharacterInfo(PersonalityDescription, configManager.GetCVar(CCVars.MaxFlavorTextLength));
        PersonalNotes = SanitizeCharacterInfo(PersonalNotes, MaxCharacterInfoLength);
        OOCNotes = SanitizeCharacterInfo(OOCNotes, MaxCharacterInfoLength);
    }

    // Inferus - Starlight's custom species name validation, moved out of the upstream EnsureValid.
    // Starlight only allows it for species with CustomName set; Inferus allows it for every species.
    private static string SanitizeCustomSpecieName(string? name, IConfigurationManager configManager, IPrototypeManager prototypeManager)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        var maxLength = Math.Min(configManager.GetCVar(CCVars.MaxNameLength), MaxCustomSpecieNameLength);
        var customspeciename = FormattedMessage.RemoveMarkupPermissive(name).Trim();
        if (customspeciename.Length > maxLength)
            customspeciename = customspeciename[..maxLength];

        if (configManager.GetCVar(StarlightCCVars.RestrictedCustomSpecieNames))
        {
            customspeciename = RestrictedCustomSpecieNameRegex.Replace(customspeciename, string.Empty);

            foreach (var species in prototypeManager.EnumeratePrototypes<SpeciesPrototype>())
            {
                if (string.Equals(Loc.GetString(species.Name), customspeciename, StringComparison.OrdinalIgnoreCase))
                    return string.Empty;
            }
        }

        return customspeciename.Trim();
    }

    private static string SanitizeCharacterInfo(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var clean = FormattedMessage.RemoveMarkupPermissive(text);
        maxLength = Math.Min(maxLength, MaxCharacterInfoLength);
        return clean.Length > maxLength ? clean[..maxLength] : clean;
    }

    private void AddStarlightCharacterInfoHash(ref HashCode hashCode)
    {
        hashCode.Add(PersonalityDescription);
        hashCode.Add(PersonalNotes);
        hashCode.Add(OOCNotes);
        hashCode.Add(CustomSpecieName);
    }
    // End Inferus
}
