// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - which foot a body steps with next. Added by CyberneticFootstepSystem when the body takes its first step, not networked,
// the client and the server both count the steps they hear.
namespace Content.Shared._Shitmed.Surgery.Organs;

[RegisterComponent]
public sealed partial class CyberneticStepComponent : Component
{
    [ViewVariables]
    public bool NextRight;
}
