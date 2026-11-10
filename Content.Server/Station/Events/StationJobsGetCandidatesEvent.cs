using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Station.Events;

[ByRefEvent]
// Inferus: distinguish configured minimum-role fallback from ordinary preferences.
public readonly record struct StationJobsGetCandidatesEvent(NetUserId Player, List<ProtoId<JobPrototype>> Jobs, bool IgnorePreferences = false);

