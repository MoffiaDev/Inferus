// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - a simple consciousness value for bodies with wounds. Goob-Station keeps it as a sum of nerve and brain modifiers,
// here it is worked out once a second from pain, blood level and head wounds.
using Content.Shared._Shitmed.Surgery.Wounds;
using Robust.Shared.GameStates;

namespace Content.Shared._Shitmed.Surgery.Consciousness;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ConsciousnessComponent : Component
{
    /// <summary>
    /// 100 is fully awake, 0 is out cold.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public float Value = 100f;

    [ViewVariables, AutoNetworkedField]
    public bool Unconscious;

    /// <summary>
    /// Below this the body passes out.
    /// </summary>
    [DataField]
    public float PassOutAt = 20f;

    /// <summary>
    /// A body that has passed out stays out until it climbs back to this.
    /// </summary>
    [DataField]
    public float WakeAt = 35f;

    /// <summary>
    /// Below this the body is told it feels woozy, as a warning.
    /// </summary>
    [DataField]
    public float WoozyAt = 50f;

    /// <summary>
    /// Pain below this costs nothing, above it every point costs <see cref="PainFactor"/>.
    /// </summary>
    [DataField]
    public float PainFree = 40f;

    [DataField]
    public float PainFactor = 0.8f;

    /// <summary>
    /// The blood level (1 is full) at which blood loss starts to cost consciousness.
    /// </summary>
    [DataField]
    public float BloodStart = 0.85f;

    /// <summary>
    /// The blood level at which blood loss alone leaves nothing.
    /// </summary>
    [DataField]
    public float BloodEnd = 0.5f;

    /// <summary>
    /// What a hurt head costs, by the severity of its wounds.
    /// </summary>
    [DataField]
    public Dictionary<WoundableSeverity, float> HeadPenalties = new()
    {
        { WoundableSeverity.Severe, 10f },
        { WoundableSeverity.Critical, 30f },
        { WoundableSeverity.Mangled, 60f },
    };

    /// <summary>
    /// How long each refresh of the forced sleep lasts. It is renewed every second while the body is out.
    /// </summary>
    [DataField]
    public TimeSpan SleepRefresh = TimeSpan.FromSeconds(6);

    public bool Woozy;
}
