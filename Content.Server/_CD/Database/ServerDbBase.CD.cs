using System.Text.Json;
using Content.Server._CD.Records;
using Content.Shared._CD.Records;
using Content.Shared.Preferences;

// ReSharper disable once CheckNamespace
namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    // Cosmatic Drift Record System-start: Persist CD record updates back onto the database profile
    private static void ConvertCDProfile(HumanoidCharacterProfile humanoid, Profile profile)
    {
        profile.CDProfile ??= new CDModel.CDProfile();
        var storedRecords = humanoid.CDCharacterRecords ?? PlayerProvidedCharacterRecords.DefaultRecords();
        profile.CDProfile.CharacterRecords = JsonSerializer.SerializeToDocument(storedRecords); // Store player-authored data as JSON for SQLite/Postgres
        profile.CDProfile.CharacterRecordEntries.Clear(); // Keep denormalized entries in sync with the serialized blob
        profile.CDProfile.CharacterRecordEntries.AddRange(RecordsSerialization.GetEntries(storedRecords));
    }
    // Cosmatic Drift Record System-end

    // Cosmatic Drift Record System: Rehydrate saved CD records into the profile
    public static HumanoidCharacterProfile LoadCDProfile(HumanoidCharacterProfile humanoid, Profile profile)
    {
        if (profile.CDProfile?.CharacterRecords != null)
        {
            var records = RecordsSerialization.Deserialize(profile.CDProfile.CharacterRecords, profile.CDProfile.CharacterRecordEntries); // Load player-authored records from storage
            return humanoid.WithCDCharacterRecords(records);
        }

        return humanoid.WithCDCharacterRecords(PlayerProvidedCharacterRecords.DefaultRecords()); // Seed with empty records when nothing has been saved yet
    }
}
