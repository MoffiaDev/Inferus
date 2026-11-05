using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Spawning;

/// <summary>
/// Sent by the client from the spawn menu to late-join as <see cref="JobId"/> on a random one of <see cref="Stations"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class ESSpawnPlayerEvent(List<NetEntity> stations, ProtoId<JobPrototype> jobId) : EntityEventArgs
{
    public List<NetEntity> Stations = stations;
    public ProtoId<JobPrototype> JobId = jobId;
}
