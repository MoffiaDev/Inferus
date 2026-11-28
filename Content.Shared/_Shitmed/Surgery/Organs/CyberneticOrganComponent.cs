// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - marks a machine organ. An EMP that hits its body or the organ itself switches it off for a while, and hurts it.
using Content.Shared.FixedPoint;

namespace Content.Shared._Shitmed.Surgery.Organs;

[RegisterComponent]
public sealed partial class CyberneticOrganComponent : Component
{
    /// <summary>
    /// How much integrity an EMP takes off the organ.
    /// </summary>
    [DataField]
    public FixedPoint2 EmpDamage = 25;

    /// <summary>
    /// Whether a switched off organ counts as lost to the body. Internal organs do, limbs only lose the use of their hands. Inferus.
    /// </summary>
    [DataField]
    public bool MarkLost = true;

    /// <summary>
    /// How much a leg that an EMP switched off slows the body down. Every such leg counts, so two of them multiply.
    /// </summary>
    [DataField]
    public float EmpSpeedModifier = 0.5f;
}

/// <summary>
/// Put on a hand organ while an EMP keeps it switched off, so the hand is taken from the body once and given back once.
/// </summary>
[RegisterComponent]
public sealed partial class EmpDisabledHandComponent : Component;

/// <summary>
/// Put on a leg or foot organ while an EMP keeps it switched off. The body is slowed for as long as it carries one.
/// </summary>
[RegisterComponent]
public sealed partial class EmpDisabledLegComponent : Component;
