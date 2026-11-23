// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Goob-Station: https://github.com/Goob-Station/Goob-Station/blob/8d81b444c5f6cab92e4756578bcf68457ec13de0/Content.Goobstation.Shared/Emoting/SharedAnimatedEmotesSystem.cs
// Flip dodging (projectile immunity) is not ported.

using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Silicons.Borgs.Components;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._Goobstation.Emoting;

public abstract partial class SharedAnimatedEmotesSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;

    public static readonly TimeSpan FlipDuration = TimeSpan.FromMilliseconds(500);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AnimatedEmotesComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<AnimatedEmotesComponent, BeforeEmoteEvent>(OnBeforeEmote);
    }

    private void OnGetState(Entity<AnimatedEmotesComponent> ent, ref ComponentGetState args)
    {
        args.State = new AnimatedEmotesComponentState(ent.Comp.Emote, ent.Comp.EmoteTime);
    }

    public void SetEmote(Entity<AnimatedEmotesComponent?> ent, ProtoId<EmotePrototype>? emote)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Emote = emote;
        ent.Comp.EmoteTime = _timing.CurTime;
        Dirty(ent);
    }

    private void OnBeforeEmote(Entity<AnimatedEmotesComponent> ent, ref BeforeEmoteEvent args)
    {
        if (args.Emote.ID != "Flip")
            return;

        var uid = ent.Owner;

        // Borgs have no standing state, they just need to be alive.
        if (HasComp<BorgChassisComponent>(uid)
            && TryComp<MobStateComponent>(uid, out var state))
        {
            if (state.CurrentState != MobState.Alive)
                args.Cancel();

            return;
        }

        if (!TryComp<StandingStateComponent>(uid, out var standing)
            || !standing.Standing
            || HasComp<KnockedDownComponent>(uid)
            || HasComp<StunnedComponent>(uid))
            args.Cancel();
    }
}
