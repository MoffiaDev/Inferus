// SPDX-License-Identifier: AGPL-3.0-or-later
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._Shitmed.Surgery.Pain;

[Serializable, NetSerializable]
public enum PainLevel
{
    None = 0,
    Moderate = 1,
    Severe = 2,
    Agony = 3, // isthistheend.ogg
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PainComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Pain;

    [ViewVariables, AutoNetworkedField]
    public PainLevel Level = PainLevel.None;

    [DataField]
    public float MaxPain = 100f;

    /// <summary>
    /// How much pain one point of wound damage causes.
    /// </summary>
    [DataField]
    public float DamageToPain = 0.8f;

    /// <summary>
    /// Pain lost per second.
    /// </summary>
    [DataField]
    public float DecayPerSecond = 1.5f;

    /// <summary>
    /// The pain value at which each level starts.
    /// </summary>
    [DataField]
    public Dictionary<PainLevel, float> Thresholds = new()
    {
        { PainLevel.Moderate, 30f },
        { PainLevel.Severe, 60f },
        { PainLevel.Agony, 90f },
    };

    /// <summary>
    /// How much more or less a hit hurts depending on the limb, by organ category. A limb that is not listed counts as 1.
    /// </summary>
    [DataField]
    public Dictionary<string, float> LimbWeights = new()
    {
        { "Head", 1.5f },
        { "HandLeft", 1.4f },
        { "HandRight", 1.4f },
        { "FootLeft", 1.4f },
        { "FootRight", 1.4f },
    };

    /// <summary>
    /// The speed multiplier at each level.
    /// </summary>
    [DataField]
    public Dictionary<PainLevel, float> SpeedMultipliers = new()
    {
        { PainLevel.Moderate, 0.9f },
        { PainLevel.Severe, 0.75f },
        { PainLevel.Agony, 0.5f },
    };

    /// <summary>
    /// How long a body is knocked down for when it reaches the level.
    /// </summary>
    [DataField]
    public Dictionary<PainLevel, TimeSpan> KnockdownTimes = new()
    {
        { PainLevel.Severe, TimeSpan.FromSeconds(2) },
        { PainLevel.Agony, TimeSpan.FromSeconds(5) },
    };

    /// <summary>
    /// While in agony the body is knocked down again this often.
    /// </summary>
    [DataField]
    public TimeSpan AgonyRepeat = TimeSpan.FromSeconds(8);

    public TimeSpan NextAgonyKnockdown;
}
