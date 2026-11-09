using Robust.Shared.GameStates;

namespace Content.Shared._Goobstation.Paint;

[RegisterComponent, NetworkedComponent]
public sealed partial class PaintingFrameComponent : Component
{
    [DataField(required: true)]
    public string Material = string.Empty;
}
