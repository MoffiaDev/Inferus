// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Body/Organ/DebrainedComponent.cs) // Inferus
using Robust.Shared.GameStates;

namespace Content.Shared._Shitmed.Surgery.Traumas;

/// <summary>
/// The body has no working brain. It can not speak or stand. Put on by the Braindeath trauma.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DebrainedComponent : Component;