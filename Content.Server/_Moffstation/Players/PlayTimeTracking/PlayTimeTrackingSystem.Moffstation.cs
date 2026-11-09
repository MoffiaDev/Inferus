using System.Linq;
using Content.Shared.CCVar;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Players.PlayTimeTracking;

public sealed partial class PlayTimeTrackingSystem
{
    /// <summary>
    /// Same as <see cref="IsAllowed(ICommonSession, ProtoId{JobPrototype})"/>, but checks a
    /// character other than the selected one. Multi-character selection has to test each of a
    /// player's active characters against the job they were assigned.
    /// </summary>
    /// <remarks>
    /// Unlike the upstream overload this still checks age, species and traits when role timers are
    /// off. Without that, picking which character spawns would ignore those gates entirely.
    /// </remarks>
    public bool IsAllowed(ICommonSession player, ProtoId<JobPrototype> job, HumanoidCharacterProfile profile)
    {
        return MoffRequirementsMet(player, _roles.GetRoleRequirements(job), profile); // Inferus
    }

    // Inferus: apply the same character-aware validation to antagonist requirements.
    public bool IsAllowed(ICommonSession player, ProtoId<AntagPrototype> antag, HumanoidCharacterProfile profile)
    {
        return MoffRequirementsMet(player, _roles.GetRoleRequirements(antag), profile);
    }

    private bool MoffRequirementsMet(ICommonSession player, HashSet<JobRequirement>? requirements, HumanoidCharacterProfile profile)
    {

        if (!_cfg.GetCVar(CCVars.GameRoleTimers))
            requirements = StripPlaytimeRequirements(requirements);

        if (requirements == null || requirements.Count == 0)
            return true;

        if (!_tracking.TryGetTrackerTimes(player, out var playTimes))
        {
            Log.Error($"Unable to check playtimes {Environment.StackTrace}");
            playTimes = new Dictionary<string, TimeSpan>();
        }

        return JobRequirements.TryRequirementsMet(requirements, playTimes, out _, EntityManager, ProtoMan, profile);
    }

    private void RemoveMoffDisallowedJobs(Robust.Shared.Network.NetUserId userId, List<ProtoId<JobPrototype>> jobs, bool ignorePreferences = false)
    {
        var session = _playerManager.GetSessionById(userId);

        // Dummy sessions and not-yet-loaded preferences retain the upstream requirement checks.
        if (!_preferencesManager.TryGetCachedPreferences(userId, out _))
        {
            jobs.RemoveAll(job => !IsAllowed(session, job));
            return;
        }

        var candidates = EntityManager.System<Content.Server._Moffstation.Station.MoffJobCandidateSystem>();
        var profiles = candidates.GetActiveProfiles(userId);
        jobs.RemoveAll(job => !profiles.Any(profile =>
            (ignorePreferences || profile.JobPriorities.ContainsKey(job)) && IsAllowed(session, job, profile)));
    }

    private HashSet<ProtoId<JobPrototype>> GetMoffDisallowedJobs(ICommonSession session)
    {
        var jobs = ProtoMan.EnumeratePrototypes<JobPrototype>().Select(job => new ProtoId<JobPrototype>(job.ID)).ToList();
        var allowed = jobs.ToList();
        // Eligibility lists must not prevent a player from selecting a previously unpreferred job.
        RemoveMoffDisallowedJobs(session.UserId, allowed, ignorePreferences: true);
        return jobs.Except(allowed).ToHashSet();
    }


    /// <summary>
    /// Drops the requirements that role timers govern, leaving the ones that describe the character
    /// itself. A null or empty result is treated as "met" by <see cref="JobRequirements"/>.
    /// </summary>
    private static HashSet<JobRequirement>? StripPlaytimeRequirements(HashSet<JobRequirement>? requirements)
    {
        if (requirements == null)
            return null;

        return requirements
            .Where(requirement => requirement is not (
                RoleTimeRequirement
                or DepartmentTimeRequirement
                or OverallPlaytimeRequirement))
            .ToHashSet();
    }
}

