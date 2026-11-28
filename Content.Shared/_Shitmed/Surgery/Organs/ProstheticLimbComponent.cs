// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - a manufactured limb. It is made on the spot as a severed limb, and the reattach surgery treats it as one,
// except that it fits any empty slot of its kind instead of the one it was cut from.
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Organs;

[RegisterComponent]
public sealed partial class ProstheticLimbComponent : Component
{
    /// <summary>
    /// The organs the limb is made of. The first one is the limb itself, the others are attached below it, like a hand on an arm.
    /// </summary>
    [DataField(required: true)]
    public List<EntProtoId> Organs = new();

    /// <summary>
    /// The category of the part the limb is attached to.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> AttachCategory;
}
