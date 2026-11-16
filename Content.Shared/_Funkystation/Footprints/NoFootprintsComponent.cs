using Robust.Shared.GameStates;

namespace Content.Shared._Funkystation.Footprints;

/// <summary>Suppresses prints on the wearer or their equipped shoes.</summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NoFootprintsComponent : Component
{
}
