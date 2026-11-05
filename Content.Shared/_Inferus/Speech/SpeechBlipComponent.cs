using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Inferus.Speech;

/// <summary>
/// Gives an entity's speech bubbles a typewriter effect with voice blips.
/// Set from the character profile for player characters, but can be added to any entity in YAML.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SpeechBlipSystem))]
public sealed partial class SpeechBlipComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<SpeechBlipPrototype> Blip = SpeechBlipSystem.DefaultBlip;
}
