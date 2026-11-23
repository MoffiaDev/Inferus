// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Goob-Station: https://github.com/Goob-Station/Goob-Station/blob/8d81b444c5f6cab92e4756578bcf68457ec13de0/Content.Goobstation.Shared/Emoting/AnimatedEmotesComponent.cs

using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Goobstation.Emoting;

// use as a template
//[Serializable, NetSerializable, DataDefinition] public sealed partial class AnimationNameEmoteEvent : EntityEventArgs { }

[Serializable, NetSerializable, DataDefinition] public sealed partial class AnimationFlipEmoteEvent : EntityEventArgs { }
[Serializable, NetSerializable, DataDefinition] public sealed partial class AnimationJumpEmoteEvent : EntityEventArgs { }

[RegisterComponent, NetworkedComponent] public sealed partial class AnimatedEmotesComponent : Component
{
    [DataField] public ProtoId<EmotePrototype>? Emote;

    [ViewVariables] public TimeSpan EmoteTime;

    [ViewVariables] public TimeSpan LastPlayedTime;
}

[Serializable, NetSerializable]
public sealed partial class AnimatedEmotesComponentState : ComponentState
{
    public ProtoId<EmotePrototype>? Emote;
    public TimeSpan EmoteTime;

    public AnimatedEmotesComponentState(ProtoId<EmotePrototype>? emote, TimeSpan emoteTime)
    {
        Emote = emote;
        EmoteTime = emoteTime;
    }
}
