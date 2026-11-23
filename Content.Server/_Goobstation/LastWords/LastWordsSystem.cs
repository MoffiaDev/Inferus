// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.LastWords;
using Content.Shared.Chat; // Goob Station
using Content.Shared.Mind;
using Content.Shared.Mobs.Components;

namespace Content.Server._Goobstation.LastWords;

public sealed partial class LastWordsSystem : EntitySystem
{
    [Dependency] private SharedMindSystem _mindSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MobStateComponent, EntitySpokeEvent>(OnEntitySpoke);
    }

    private void OnEntitySpoke(EntityUid uid, MobStateComponent _, EntitySpokeEvent args)
    {
        // Inferus - whispers (they carry an obfuscated message) are never recorded as last words.
        if (args.ObfuscatedMessage != null)
            return;

        SetLastWords(uid, args.Message);
    }

    // Inferus - helper so other systems (crit last words) can record last words explicitly
    /// <summary>
    /// Records <paramref name="message"/> as the last words of the mind controlling <paramref name="uid"/>.
    /// </summary>
    public void SetLastWords(EntityUid uid, string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (!_mindSystem.TryGetMind(uid, out var mindId, out _))
            return;

        EnsureComp<LastWordsComponent>(mindId).LastWords = message;
    }
}
