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
}
