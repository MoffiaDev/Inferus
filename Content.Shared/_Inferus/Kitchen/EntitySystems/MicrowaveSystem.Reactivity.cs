using Content.Shared._Inferus.Kitchen;

// This namespace must match the upstream partial class; the extension itself belongs to Inferus.
namespace Content.Shared.Kitchen.EntitySystems;

public abstract partial class MicrowaveSystem
{
    [Dependency] private MicrowaveReactivitySystem _inferusReactivity = default!;
}
