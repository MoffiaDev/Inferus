using Robust.Shared.GameStates;

namespace Content.Shared._Inferus.Audio;

/// <summary>
/// Shifts the pitch of sounds an entity makes, for example making small creatures sound higher.
/// Values are multipliers on top of the sound's normal pitch, so 1 means unchanged.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SoundPitchModifierComponent : Component
{
    /// <summary>
    /// Pitch multiplier for the entity's footsteps.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Footsteps = 1f;

    /// <summary>
    /// Pitch multiplier for the entity's speech blips, applied on top of the character's own blip pitch.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SpeechBlips = 1f;
}
