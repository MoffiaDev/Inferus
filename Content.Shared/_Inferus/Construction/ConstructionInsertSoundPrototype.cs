using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Inferus.Construction;

/// <summary>
/// Maps the kind of item being placed during construction (into a construction graph step, a machine frame,
/// or a crafting recipe) to the "place" sound that should play.
/// Rules are checked from highest <see cref="Priority"/> to lowest; the first match wins.
/// If nothing matches, the rule marked <see cref="Fallback"/> is used.
/// </summary>
[Prototype]
public sealed partial class ConstructionInsertSoundPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The sound to play when a matching item is placed.
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier Sound = default!;

    /// <summary>
    /// Higher priority rules are checked first.
    /// </summary>
    [DataField]
    public int Priority;

    /// <summary>
    /// Used when no other rule matches.
    /// </summary>
    [DataField]
    public bool Fallback;

    /// <summary>
    /// Stack types (materials) that match this rule.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<StackPrototype>> Stacks = new();

    /// <summary>
    /// Tags that match this rule exactly.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<TagPrototype>> Tags = new();

    /// <summary>
    /// Case-insensitive substrings matched against tag IDs, e.g. "Electronics" matches "DoorElectronics".
    /// </summary>
    [DataField]
    public List<string> TagPatterns = new();

    /// <summary>
    /// Component names that match this rule exactly, e.g. "MachineBoard".
    /// </summary>
    [DataField]
    public HashSet<string> Components = new();

    /// <summary>
    /// Case-insensitive substrings matched against component names, e.g. "Electronics" matches "DoorElectronics".
    /// </summary>
    [DataField]
    public List<string> ComponentPatterns = new();

    /// <summary>
    /// Pitch variation applied to the sound, same as the construction PlaySound action.
    /// </summary>
    [DataField]
    public float Variation = 0.125f;
}
