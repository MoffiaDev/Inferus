// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (Content.Server/_Starlight/Emp/EmpOnMeleeHitComponent.cs) // Inferus
// Inferus - a melee weapon that sends out an EMP pulse at whatever it hits. The weapon has to be on if it can be toggled.
namespace Content.Shared._Shitmed.Emp;

[RegisterComponent]
public sealed partial class EmpOnMeleeHitComponent : Component
{
    /// <summary>
    /// How far from the entity that was hit the pulse reaches.
    /// </summary>
    [DataField]
    public float Range = 1f;

    /// <summary>
    /// How much energy the pulse takes from batteries in range, in joules.
    /// </summary>
    [DataField]
    public float EnergyConsumption;

    /// <summary>
    /// How long the pulse disables what it hits.
    /// </summary>
    [DataField]
    public TimeSpan DisableDuration = TimeSpan.FromSeconds(60);
}
