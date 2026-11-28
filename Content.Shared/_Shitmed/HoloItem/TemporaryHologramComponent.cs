// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - a hologram that is deleted after a time, and when it is taken out of the container it was put in (the cuffs of a person who wriggles free).
// The server does the deleting, the component is shared so that the client knows it.
namespace Content.Shared._Shitmed.HoloItem;

[RegisterComponent]
public sealed partial class TemporaryHologramComponent : Component
{
    /// <summary>
    /// How long the hologram lasts, wherever it is. Not even a container keeps it.
    /// </summary>
    [DataField]
    public TimeSpan Lifetime = TimeSpan.FromSeconds(180);

    [ViewVariables]
    public TimeSpan ExpiresAt;
}
