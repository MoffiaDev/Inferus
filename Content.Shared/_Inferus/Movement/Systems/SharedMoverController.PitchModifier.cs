using Content.Shared._Inferus.Audio;
using Robust.Shared.Audio;

// ReSharper disable once CheckNamespace
namespace Content.Shared.Movement.Systems;

public abstract partial class SharedMoverController
{
    /// <summary>
    /// Applies <see cref="SoundPitchModifierComponent.Footsteps"/> to a footstep sound.
    /// </summary>
    protected AudioParams ApplyFootstepPitch(EntityUid uid, AudioParams audioParams)
    {
        if (!TryComp(uid, out SoundPitchModifierComponent? modifier))
            return audioParams;

        return audioParams.WithPitchScale(audioParams.Pitch * modifier.Footsteps);
    }
}
