using System.Linq;
using Content.Server.Antag;
using Content.Server.Players.PlayTimeTracking;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Moffstation.Station;

/// <summary>
/// Picks which of a player's active characters spawns, once a job has been assigned to them.
/// </summary>
public sealed partial class MoffCharacterPickerSystem : EntitySystem
{
    [Dependency] private AntagSelectionSystem _antag = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MoffJobCandidateSystem _candidates = default!;
    [Dependency] private PlayTimeTrackingSystem _playTime = default!;

    /// <summary>
    /// So antag loadouts equip the character that spawned, not the one selected in the lobby.
    /// </summary>
    private readonly Dictionary<NetUserId, HumanoidCharacterProfile> _spawnedProfiles = new();

    /// <summary>
    /// A late join names the character it wants, so nothing should pick one at random for it.
    /// </summary>
    private readonly Dictionary<NetUserId, HumanoidCharacterProfile> _explicitChoices = new();

    [SubscribeLocalEvent]
    private void OnCleanup(RoundRestartCleanupEvent ev)
    {
        _spawnedProfiles.Clear();
        _explicitChoices.Clear();
    }

    /// <summary>Records the character a late join asked to spawn as.</summary>
    public void SetExplicitChoice(NetUserId player, HumanoidCharacterProfile profile)
    {
        _explicitChoices[player] = profile;
    }

    public void ClearExplicitChoice(NetUserId player) // Inferus: failed late joins must not leave pending choices.
    {
        _explicitChoices.Remove(player);
    }

    /// <summary>
    /// Returns and consumes a pending explicit choice, so it applies to exactly one spawn.
    /// </summary>
    public HumanoidCharacterProfile? TakeExplicitChoice(NetUserId player)
    {
        if (!_explicitChoices.Remove(player, out var profile))
            return null;

        _spawnedProfiles[player] = profile;
        return profile;
    }

    /// <summary>
    /// Returns null when no active character meets both the job and preselected-antag requirements.
    /// The caller must leave the player in the lobby rather than bypass those requirements.
    /// </summary>
    public HumanoidCharacterProfile? PickProfile(ICommonSession player, ProtoId<JobPrototype> job, bool ignorePreferences = false)
    {
        // Inferus: the configured minimum-role fallback may assign an unpreferred role.
        var eligible = ignorePreferences
            ? _candidates.GetActiveProfiles(player.UserId)
            : _candidates.GetEligibleProfiles(player.UserId, job);


        if (eligible.Count == 0)
            return null;

        // Drop characters that don't meet the job's own requirements, e.g. age or species. This
        // goes through PlayTimeTrackingSystem so that disabled role timers are honored.
        var allowed = eligible.Where(profile => _playTime.IsAllowed(player, job, profile)).ToList();

        if (allowed.Count == 0)
        {
            Log.Warning($"No active character of {player} meets the requirements for {job}."); // Inferus: never bypass job requirements.
            return null; // Inferus
        }

        // A preselected antag should be filled by a character that opted in to it.
        var final = allowed;

        foreach (var antagSet in _antag.GetMoffPreSelectedAntagPrefRoles(player))
        {
            final = final.Where(profile => antagSet.Any(antag =>
                profile.AntagPreferences.Contains(antag) && _playTime.IsAllowed(player, antag, profile))).ToList(); // Inferus
        }

        if (final.Count == 0)
        {
            Log.Warning($"No active character of {player} wants the antag role they were preselected for.");
            return null;
        }

        // Inferus: prefer willing characters after all job and antag requirements are satisfied.
        if (ignorePreferences)
        {
            var preferred = final.Where(profile => profile.JobPriorities.ContainsKey(job)).ToList();
            if (preferred.Count > 0)
                final = preferred;
        }


        var picked = _random.Pick(final);
        _spawnedProfiles[player.UserId] = picked;

        return picked;
    }

    /// <summary>For picking a job when the caller has not assigned one, e.g. late joins.</summary>
    public Dictionary<ProtoId<JobPrototype>, JobPriority> GetJobPriorities(
        NetUserId player,
        HumanoidCharacterProfile fallback)
    {
        return _candidates.GetJobPriorities(player, fallback);
    }

    /// <summary>Null if they have not spawned this round.</summary>
    public HumanoidCharacterProfile? GetSpawnedProfile(NetUserId player)
    {
        return _spawnedProfiles.GetValueOrDefault(player);
    }
}

