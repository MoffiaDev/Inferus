// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Goob-Station: https://github.com/Goob-Station/Goob-Station/blob/8d81b444c5f6cab92e4756578bcf68457ec13de0/Content.Goobstation.Client/Emoting/AnimatedEmotesSystem.cs

using System.Numerics;
using Content.Shared._Goobstation.Emoting;
using Content.Shared.Chat.Prototypes;
using Robust.Client.Animations;
using Robust.Client.GameObjects;
using Robust.Shared.Animations;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Goobstation.Emoting;

public sealed partial class AnimatedEmotesSystem : SharedAnimatedEmotesSystem
{
    [Dependency] private AnimationPlayerSystem _anim = default!;
    [Dependency] private IPrototypeManager _prot = default!;
    [Dependency] private IGameTiming _timing = default!;

    private static readonly TimeSpan MaxEmoteAge = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimatedEmotesComponent, ComponentHandleState>(OnHandleState);

        SubscribeLocalEvent<AnimatedEmotesComponent, AnimationFlipEmoteEvent>(OnFlip);
        SubscribeLocalEvent<AnimatedEmotesComponent, AnimationJumpEmoteEvent>(OnJump);
    }

    public void PlayEmote(EntityUid uid, Animation anim, string animationKey = "emoteAnimKeyId")
    {
        if (_anim.HasRunningAnimation(uid, animationKey))
            return;

        _anim.Play(uid, anim, animationKey);
    }

    private void OnHandleState(EntityUid uid, AnimatedEmotesComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not AnimatedEmotesComponentState state)
            return;

        var fresh = state.EmoteTime > component.LastPlayedTime && _timing.CurTime - state.EmoteTime <= MaxEmoteAge;
        component.Emote = state.Emote;
        component.EmoteTime = state.EmoteTime;
        component.LastPlayedTime = state.EmoteTime;

        if (!fresh || !_prot.TryIndex<EmotePrototype>(state.Emote, out var emote))
            return;

        if (emote.Event != null)
            RaiseLocalEvent(uid, emote.Event);
    }

    private void OnFlip(Entity<AnimatedEmotesComponent> ent, ref AnimationFlipEmoteEvent args)
    {
        var a = new Animation
        {
            Length = TimeSpan.FromMilliseconds(500),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Rotation),
                    InterpolationMode = AnimationInterpolationMode.Linear,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Angle.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(180), 0.25f),
                        new AnimationTrackProperty.KeyFrame(Angle.FromDegrees(360), 0.25f),
                    }
                }
            }
        };
        PlayEmote(ent, a);
    }

    private void OnJump(Entity<AnimatedEmotesComponent> ent, ref AnimationJumpEmoteEvent args)
    {
        var a = new Animation
        {
            Length = TimeSpan.FromMilliseconds(250),
            AnimationTracks =
            {
                new AnimationTrackComponentProperty
                {
                    ComponentType = typeof(SpriteComponent),
                    Property = nameof(SpriteComponent.Offset),
                    InterpolationMode = AnimationInterpolationMode.Cubic,
                    KeyFrames =
                    {
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0f),
                        new AnimationTrackProperty.KeyFrame(new Vector2(0, .35f), 0.125f),
                        new AnimationTrackProperty.KeyFrame(Vector2.Zero, 0.125f),
                    }
                }
            }
        };
        PlayEmote(ent, a);
    }
}
