using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.Eye.Blinking;
using Robust.Shared.Audio.Systems; // Inferus

namespace Content.Server.Eye.Blinking;

/// <inheritdoc/>
public sealed partial class ServerEyeBlinkingSystem : EyeBlinkingSystem
{
    [Dependency] private SharedAudioSystem _audio = default!; // Inferus - blink emote sound

    // TODO: Move all this stuff into Shared once chat is predicted.
    [SubscribeLocalEvent]
    private void OnEmote(Entity<EyeBlinkingComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = TrySendEmoteMessage(ent, args.Emote.ID);
    }

    [SubscribeLocalEvent]
    private void OnEmote(Entity<EyeBlinkingComponent> ent, ref BodyRelayedEvent<EmoteEvent> args)
    {
        if (args.Args.Handled)
            return;

        var handled = TrySendEmoteMessage(ent, args.Args.Emote.ID);
        args.Args = args.Args with { Handled = handled };
    }

    private bool TrySendEmoteMessage(Entity<EyeBlinkingComponent> ent, string emoteId)
    {
        if (!ent.Comp.BlinkEmoteId.Contains(emoteId))
            return false;

        if (ent.Comp.Status != BlinkStatus.Normal)
            return false;

        var ev = new BlinkEyeEvent(GetNetEntity(ent.Owner));
        RaiseNetworkEvent(ev);
        _audio.PlayPvs(ent.Comp.BlinkEmoteSound, GetActiveEntity(ent)); // Inferus - blink emote sound
        return true;
    }
}
