using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Inferus.Speech;

/// <summary>
/// A selectable "voice blip" that controls how a character's speech bubbles appear.
/// Typewriter blips reveal speech one letter at a time and play <see cref="Sound"/> as letters appear.
/// </summary>
[Prototype]
public sealed partial class SpeechBlipPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Localized name shown in the character editor.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Used to sort the blips in the character editor. Lower comes first.
    /// </summary>
    [DataField]
    public int Order;

    /// <summary>
    /// If false, speech bubbles appear instantly like they normally would and no blips are played.
    /// </summary>
    [DataField]
    public bool Typewriter = true;

    /// <summary>
    /// The sound played as letters appear. If this is a sound collection, each file in it becomes its own clip.
    /// Can be combined with <see cref="Sounds"/>. If neither is set, the text types out silently.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Extra clips to pick between as letters appear. Each one can have its own params, like pitch or volume.
    /// </summary>
    [DataField]
    public List<SoundSpecifier> Sounds = new();

    /// <summary>
    /// How a clip is chosen when there's more than one.
    /// </summary>
    [DataField]
    public SpeechBlipSelection Selection = SpeechBlipSelection.Random;

    /// <summary>
    /// Seconds between each letter appearing.
    /// </summary>
    [DataField]
    public float CharacterDelay = 0.035f;

    /// <summary>
    /// Seconds the text halts for after a pause character, like a comma or period.
    /// </summary>
    [DataField]
    public float PauseDelay = 0.25f;

    /// <summary>
    /// Minimum seconds between two blips, so fast text doesn't turn into noise.
    /// </summary>
    [DataField]
    public float BlipCooldown = 0.06f;

    /// <summary>
    /// How far the pitch moves over a sentence. Questions rise by this much by the end,
    /// exclamations start this much higher and fall back down. 0.15 is 15%.
    /// </summary>
    [DataField]
    public float IntonationPitch = 0.25f;
}

public enum SpeechBlipSelection : byte
{
    /// <summary>
    /// A random clip each time, never the same one twice in a row.
    /// </summary>
    Random,

    /// <summary>
    /// Goes through the clips in order, looping back to the start.
    /// </summary>
    Sequential,

    /// <summary>
    /// Each letter always uses the same clip, so the same words always sound the same.
    /// </summary>
    Letter,
}
