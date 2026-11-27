// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Server/_Shitmed/Targeting/TargetingSystem.cs) // Inferus
// Inferus - mob-state/BodyStatus handling depended on Goob's WoundSystem and is added in stage 2.
using Content.Shared._Shitmed.Targeting;
using Content.Shared._Shitmed.Targeting.Events;
using Robust.Shared.Player;

namespace Content.Server._Shitmed.Targeting;

public sealed class TargetingSystem : SharedTargetingSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<TargetChangeEvent>(OnTargetChange);
    }

    private void OnTargetChange(TargetChangeEvent message, EntitySessionEventArgs args)
    {
        // Inferus - never trust the entity sent by the client, only the sender's own entity may be retargeted.
        if (args.SenderSession.AttachedEntity is not { } uid
            || !TryComp<TargetingComponent>(uid, out var targeting)
            || !Enum.IsDefined(message.BodyPart)
            || !Array.Exists(GetValidParts(), p => p == message.BodyPart))
            return;

        targeting.Target = message.BodyPart;
        Dirty(uid, targeting);
    }
}
