using Robust.Shared.Network;

namespace Content.Server._Floof.Consent;

// Inferus - Remembers which character slot a body was spawned from, so in-round consent changes save to it
[RegisterComponent]
public sealed partial class ConsentProfileSlotComponent : Component
{
    [ViewVariables]
    public NetUserId User;

    [ViewVariables]
    public int Slot;
}
