using Robust.Shared.Serialization;

namespace Content.Shared._Funkystation.Footprints;

// Support events referenced by the existing Funky server and client systems.
[Serializable, NetSerializable]
public sealed partial class FootprintStateEvent : EntityEventArgs
{
    public NetEntity NetEntity;

    public FootprintStateEvent(NetEntity netEntity)
    {
        NetEntity = netEntity;
    }
}

public sealed class FootprintCleanEvent : EntityEventArgs
{
}
