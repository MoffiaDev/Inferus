// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Targeting/SharedTargetingSystem.cs) // Inferus
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Targeting;

public abstract class SharedTargetingSystem : EntitySystem
{
    private static readonly TargetBodyPart[] ValidParts =
    {
        TargetBodyPart.Head,
        TargetBodyPart.Chest,
        TargetBodyPart.Groin,
        TargetBodyPart.LeftArm,
        TargetBodyPart.LeftHand,
        TargetBodyPart.LeftLeg,
        TargetBodyPart.LeftFoot,
        TargetBodyPart.RightArm,
        TargetBodyPart.RightHand,
        TargetBodyPart.RightLeg,
        TargetBodyPart.RightFoot,
    };

    /// <summary>
    /// Returns all valid target body parts as an array.
    /// </summary>
    public static TargetBodyPart[] GetValidParts() => ValidParts;

    /// <summary>
    /// Maps a target part to the organ category that represents it on an Inferus body.
    /// Groin has no category of its own and resolves to Torso.
    /// </summary>
    public static ProtoId<OrganCategoryPrototype> ToOrganCategory(TargetBodyPart part)
    {
        return part switch
        {
            TargetBodyPart.Head => "Head",
            TargetBodyPart.LeftArm => "ArmLeft",
            TargetBodyPart.LeftHand => "HandLeft",
            TargetBodyPart.RightArm => "ArmRight",
            TargetBodyPart.RightHand => "HandRight",
            TargetBodyPart.LeftLeg => "LegLeft",
            TargetBodyPart.LeftFoot => "FootLeft",
            TargetBodyPart.RightLeg => "LegRight",
            TargetBodyPart.RightFoot => "FootRight",
            _ => "Torso",
        };
    }
}
