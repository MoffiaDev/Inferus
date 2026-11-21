using Content.Shared._CD.Records;

// ReSharper disable once CheckNamespace
namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    // Cosmatic Drift – stores the player's custom record data on the profile itself.
    [DataField("cosmaticDriftCharacterRecords")]
    public PlayerProvidedCharacterRecords? CDCharacterRecords { get; private set; } = PlayerProvidedCharacterRecords.DefaultRecords();

    // Cosmatic Drift Record System-start
    public HumanoidCharacterProfile WithCDCharacterRecords(PlayerProvidedCharacterRecords records)
    {
        var copy = new PlayerProvidedCharacterRecords(records);
        copy.EnsureValid();
        return new HumanoidCharacterProfile(this) { CDCharacterRecords = copy };
    }
    // Cosmatic Drift Record System-end

    // Inferus - helpers so the upstream profile file only needs one-line hooks.
    private void CopyCDCharacterRecords(HumanoidCharacterProfile other)
    {
        CDCharacterRecords = other.CDCharacterRecords != null
            ? new PlayerProvidedCharacterRecords(other.CDCharacterRecords)
            : PlayerProvidedCharacterRecords.DefaultRecords();
        CDCharacterRecords.EnsureValid();
    }

    private bool CDCharacterRecordsEquals(HumanoidCharacterProfile other)
    {
        if (CDCharacterRecords != null)
            return other.CDCharacterRecords != null && CDCharacterRecords.MemberwiseEquals(other.CDCharacterRecords);

        return other.CDCharacterRecords == null;
    }

    private void EnsureValidCDCharacterRecords()
    {
        CDCharacterRecords ??= PlayerProvidedCharacterRecords.DefaultRecords();
        CDCharacterRecords.EnsureValid();
    }
    // End Inferus
}
