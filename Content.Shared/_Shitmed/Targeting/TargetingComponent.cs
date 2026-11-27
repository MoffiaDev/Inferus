// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Goob-Station (Content.Shared/_Shitmed/Targeting/TargetingComponent.cs) // Inferus
// Inferus - wound/BodyStatus tracking removed here, it is added back in stage 2 on top of organs.
using Robust.Shared.GameStates;

namespace Content.Shared._Shitmed.Targeting;

/// <summary>
/// Controls entity limb targeting for actions.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TargetingComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public TargetBodyPart Target = TargetBodyPart.Chest;

    /// <summary>
    /// What odds are there for every part targeted to be hit?
    /// </summary>
    [DataField]
    public Dictionary<TargetBodyPart, Dictionary<TargetBodyPart, float>> TargetOdds = new()
    {
        {
            TargetBodyPart.Head, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.Head, 0.5f },
                { TargetBodyPart.Chest, 0.5f },
            }
        },
        {
            TargetBodyPart.Chest, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.Chest, 1f },
            }
        },
        {
            TargetBodyPart.Groin, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.Groin, 0.3f },
                { TargetBodyPart.Chest, 0.7f },
            }
        },
        {
            TargetBodyPart.RightArm, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.RightArm, 0.7f },
                { TargetBodyPart.Chest, 0.3f },
            }
        },
        {
            TargetBodyPart.LeftArm, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.LeftArm, 0.7f },
                { TargetBodyPart.Chest, 0.3f },
            }
        },
        {
            TargetBodyPart.RightHand, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.RightHand, 0.1f },
                { TargetBodyPart.Chest, 0.5f },
                { TargetBodyPart.RightArm, 0.4f },
            }
        },
        {
            TargetBodyPart.LeftHand, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.LeftHand, 0.1f },
                { TargetBodyPart.Chest, 0.5f },
                { TargetBodyPart.LeftArm, 0.4f },
            }
        },
        {
            TargetBodyPart.RightLeg, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.RightLeg, 0.7f },
                { TargetBodyPart.Chest, 0.3f },
            }
        },
        {
            TargetBodyPart.LeftLeg, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.LeftLeg, 0.7f },
                { TargetBodyPart.Chest, 0.3f },
            }
        },
        {
            TargetBodyPart.RightFoot, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.RightFoot, 0.1f },
                { TargetBodyPart.Chest, 0.5f },
                { TargetBodyPart.RightLeg, 0.4f },
            }
        },
        {
            TargetBodyPart.LeftFoot, new Dictionary<TargetBodyPart, float>
            {
                { TargetBodyPart.LeftFoot, 0.1f },
                { TargetBodyPart.Chest, 0.5f },
                { TargetBodyPart.LeftLeg, 0.4f },
            }
        },
    };

    /// <summary>
    /// What noise does the entity play when swapping targets?
    /// </summary>
    [DataField]
    public string SwapSound = "/Audio/Effects/toggleoncombat.ogg";
}
