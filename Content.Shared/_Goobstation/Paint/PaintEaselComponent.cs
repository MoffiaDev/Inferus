using Robust.Shared.GameStates;

namespace Content.Shared._Goobstation.Paint;

[RegisterComponent, NetworkedComponent]
public sealed partial class PaintEaselComponent : Component
{
    [DataField]
    public string Slot = "canvas";

    [DataField]
    public float CanvasScale = 0.75f;
}
