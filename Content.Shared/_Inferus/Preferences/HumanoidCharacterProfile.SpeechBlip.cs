using Content.Shared._Inferus.Speech;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    /// <summary>
    /// The voice blip used for this character's speech bubbles.
    /// </summary>
    [DataField]
    public ProtoId<SpeechBlipPrototype> SpeechBlip { get; set; } = SpeechBlipSystem.DefaultBlip;

    /// <summary>
    /// Pitch multiplier applied to this character's speech blips.
    /// Clamped between <see cref="SpeechBlipSystem.MinPitch"/> and <see cref="SpeechBlipSystem.MaxPitch"/>.
    /// </summary>
    [DataField]
    public float SpeechBlipPitch { get; set; } = SpeechBlipSystem.DefaultPitch;

    public HumanoidCharacterProfile WithSpeechBlipPitch(float pitch)
    {
        return new(this) { SpeechBlipPitch = SpeechBlipSystem.ClampPitch(pitch) };
    }

    public HumanoidCharacterProfile WithSpeechBlip(ProtoId<SpeechBlipPrototype> speechBlip)
    {
        return new(this) { SpeechBlip = speechBlip };
    }

    private void EnsureValidSpeechBlip(IPrototypeManager prototypeManager)
    {
        if (!prototypeManager.HasIndex(SpeechBlip))
            SpeechBlip = SpeechBlipSystem.DefaultBlip;

        SpeechBlipPitch = SpeechBlipSystem.ClampPitch(SpeechBlipPitch);
    }
}
