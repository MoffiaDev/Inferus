// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (MovementBodyPart, MovementBodyPartHinderedByShoes) // Inferus
// Inferus - what a cybernetic leg does to the speed of the body.
namespace Content.Shared._Shitmed.Surgery.Organs;

[RegisterComponent]
public sealed partial class CyberneticLegComponent : Component
{
    /// <summary>
    /// How fast the leg walks compared to an organic one.
    /// </summary>
    [DataField]
    public float WalkModifier = 1f;

    /// <summary>
    /// How fast the leg sprints compared to an organic one.
    /// </summary>
    [DataField]
    public float SprintModifier = 1f;

    /// <summary>
    /// How much of the sprint speed a pair of shoes takes away. The legs of the body add up, so two legs with 0.1 take 20%.
    /// </summary>
    [DataField]
    public float ShoeHinder;
}
