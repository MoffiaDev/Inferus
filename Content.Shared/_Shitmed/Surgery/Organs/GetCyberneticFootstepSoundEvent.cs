// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - raised on a body by the mover controller when it picks the sound of a footstep, before the shoes are looked at.
using Robust.Shared.Audio;

namespace Content.Shared._Shitmed.Surgery.Organs;

/// <summary>
/// Set <see cref="Sound"/> to make the step of the body sound like that, instead of the shoes or the floor.
/// </summary>
[ByRefEvent]
public record struct GetCyberneticFootstepSoundEvent(SoundSpecifier? Sound = null);
