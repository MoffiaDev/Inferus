// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (Content.Shared/_Starlight/Paper/MultistampComponent.cs) // Inferus
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._Shitmed.Paper;

/// <summary>
/// A stamp that copies the look of one of the stamps put inside it. Activate it to go to the next stamp.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class MultistampComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> Stamps = new();

    [ViewVariables, AutoNetworkedField]
    public int CurrentEntry;

    [ViewVariables, AutoNetworkedField]
    public string CurrentStampName = string.Empty;

    [DataField]
    public SoundSpecifier? ChangeSound = new SoundPathSpecifier("/Audio/Machines/lightswitch.ogg");
}
