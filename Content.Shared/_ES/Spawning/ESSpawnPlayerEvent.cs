using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Spawning;

/// <summary>
/// Sent by the client from the spawn menu to late-join as <see cref="JobId"/> on a random station with an open slot.
/// </summary>
[Serializable, NetSerializable]
public sealed class ESSpawnPlayerEvent(ProtoId<JobPrototype> jobId) : EntityEventArgs
{
    public ProtoId<JobPrototype> JobId = jobId;
}
