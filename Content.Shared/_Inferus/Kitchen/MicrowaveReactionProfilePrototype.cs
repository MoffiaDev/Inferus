using System.Collections.Generic;
using Content.Shared.Materials;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._Inferus.Kitchen;

/// <summary>
/// Shared, data-driven classification for generally metallic or electronic microwave contents.
/// </summary>
[Prototype]
public sealed partial class MicrowaveReactionProfilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Existing tags and electronic component markers. Any match is sufficient.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = default!;

    /// <summary>
    /// Metals in an item's physical composition. Descendants of these materials also qualify.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<MaterialPrototype>> Metals = new();
}
