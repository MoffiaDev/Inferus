// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Server/_Shitmed/Body/Systems/DebrainedSystem.cs) // Inferus
// Inferus - without a brain the body falls down and can not stand or speak. Goob also stuns it and starts a delayed death,
// and blocks reviving and breathing in base code, none of that is here.
using Content.Server.Popups;
using Content.Shared._Shitmed.Surgery.Traumas;
using Content.Shared.Speech;
using Content.Shared.Standing;

namespace Content.Server._Shitmed.Surgery.Traumas;

public sealed class DebrainedSystem : EntitySystem
{
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private StandingStateSystem _standing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DebrainedComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<DebrainedComponent, SpeakAttemptEvent>(OnSpeakAttempt);
        SubscribeLocalEvent<DebrainedComponent, StandAttemptEvent>(OnStandAttempt);
    }

    private void OnInit(Entity<DebrainedComponent> ent, ref ComponentInit args)
    {
        if (!TerminatingOrDeleted(ent))
            _standing.Down(ent);
    }

    private void OnSpeakAttempt(Entity<DebrainedComponent> ent, ref SpeakAttemptEvent args)
    {
        _popup.PopupEntity(Loc.GetString("speech-muted"), ent, ent);
        args.Cancel();
    }

    private void OnStandAttempt(Entity<DebrainedComponent> ent, ref StandAttemptEvent args)
    {
        args.Cancel();
    }
}