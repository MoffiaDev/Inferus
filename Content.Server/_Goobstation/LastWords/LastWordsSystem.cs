// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.LastWords;
using Content.Shared.Chat; // Goob Station
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;

namespace Content.Server._Goobstation.LastWords;

public sealed class LastWordsSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, EntitySpokeEvent>(OnEntitySpoke);
    }

    private void OnEntitySpoke(EntityUid uid, MobStateComponent _, EntitySpokeEvent args)
    {
        _mindSystem.TryGetMind(uid, out var mindId, out var _);

        if (TryComp<LastWordsComponent>(mindId, out var lastWordsComp))
            lastWordsComp.LastWords = args.Message;
    }
}
