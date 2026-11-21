// SPDX-FileCopyrightText: 2025 Starlight
// SPDX-License-Identifier: Starlight-MIT

using Content.Shared.Preferences;

// ReSharper disable once CheckNamespace
namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    private static void ConvertStarlightCharacterInfo(HumanoidCharacterProfile humanoid, Profile profile)
    {
        profile.CharacterInfo ??= new StarLightModel.CharacterInfo();
        profile.CharacterInfo.PersonalityDesc = humanoid.PersonalityDescription;
        profile.CharacterInfo.PersonalNotes = humanoid.PersonalNotes;
        profile.CharacterInfo.OOCNotes = humanoid.OOCNotes;

        profile.StarLightProfile ??= new StarLightModel.StarLightProfile();
        profile.StarLightProfile.CustomSpecieName = humanoid.CustomSpecieName;
    }

    public static HumanoidCharacterProfile LoadStarlightCharacterInfo(HumanoidCharacterProfile humanoid, Profile profile)
    {
        humanoid = humanoid.WithCustomSpecieName(profile.StarLightProfile?.CustomSpecieName ?? "");

        if (profile.CharacterInfo == null)
            return humanoid;

        return humanoid
            .WithPersonalityDesc(profile.CharacterInfo.PersonalityDesc)
            .WithPersonalNotes(profile.CharacterInfo.PersonalNotes)
            .WithOOCNotes(profile.CharacterInfo.OOCNotes);
    }
}
