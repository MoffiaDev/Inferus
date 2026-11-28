// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - the room reverb of a body whose ears were taken out goes through a low-pass filter too. The reverb is sent through
// an auxiliary slot, which the direct (occlusion) filter does not touch, so it needs its own muffle.
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Robust.Client.Player;

namespace Content.Client._Starlight.Audio;

public sealed partial class RoomReverbSystem
{
    /// <summary>
    /// What the high frequencies of the reverb are multiplied by when the ears are gone.
    /// </summary>
    private const float DeafReverbGainHF = 0.0275f;

    private bool _muffled;

    private bool HasLostEars()
    {
        return TryComp(_player.LocalEntity, out LostOrgansComponent? lost) && lost.Categories.Contains("Ears");
    }
}
