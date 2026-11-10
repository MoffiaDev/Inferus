using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.Traits;

// Inferus: keep existing YAML and stored trait IDs while adding the condition/effect system.
public sealed partial class TraitPrototype
{
    [DataField] public EntityWhitelist? Whitelist;
    [DataField] public EntityWhitelist? Blacklist;
    [DataField] public ComponentRegistry Components = new();
    [DataField(serverOnly: true)] public List<JobSpecial> Specials = new();
    [DataField] public EntProtoId? TraitGear;
}
