namespace Content.Shared._Funkystation.Footprints;

[RegisterComponent]
public sealed partial class FootprintOwnerComponent : Component
{
    [DataField] public float MaxFootVolume = 10f;
    [DataField] public float MaxBodyVolume = 20f;
    [DataField] public float MinPrintVolume = 0.5f;
    [DataField] public float MaxFootprintVolume = 1f;
    [DataField] public float MinBodyPrintVolume = 2f;
    [DataField] public float MaxBodyprintVolume = 5f;
    [DataField] public float FootstepDistance = 0.5f;
    [DataField] public float DragDistance = 1f;

    [ViewVariables(VVAccess.ReadWrite)]
    public float DistanceWalked;

    [DataField] public float AlternateStepOffset = 0.0625f;

    /// <summary>Inferus - RSI state used for the next left barefoot print.</summary>
    [DataField] public string LeftBarePrint = "footprint-left-bare-human";

    /// <summary>Inferus - RSI state used for the next right barefoot print.</summary>
    [DataField] public string RightBarePrint = "footprint-right-bare-human";

    /// <summary>Inferus - Tracks the side of the next deposited standing print.</summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool RightStep = true;
}
