// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Goob-Station: https://github.com/Goob-Station/Goob-Station/blob/8d81b444c5f6cab92e4756578bcf68457ec13de0/Content.Goobstation.Server/Emoting/AnimatedEmotesSystem.cs

using Content.Shared._Goobstation.Emoting;
using Content.Shared.Chat;

namespace Content.Server._Goobstation.Emoting;

public sealed partial class AnimatedEmotesSystem : SharedAnimatedEmotesSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimatedEmotesComponent, EmoteEvent>(OnEmote);
    }

    private void OnEmote(Entity<AnimatedEmotesComponent> ent, ref EmoteEvent args)
    {
        SetEmote(ent.AsNullable(), args.Emote.ID);
    }
}
