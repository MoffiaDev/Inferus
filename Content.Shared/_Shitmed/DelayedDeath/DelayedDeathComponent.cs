// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Goob-Station (Content.Server/_Shitmed/DelayedDeath/DelayedDeathComponent.cs) // Inferus
// Inferus - lives in Content.Shared so WoundSystem can start it when a heart is taken out. Only the server system acts on it.
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.DelayedDeath;

[RegisterComponent]
public sealed partial class DelayedDeathComponent : Component
{
    /// <summary>
    /// How long it takes to kill the entity.
    /// </summary>
    [DataField]
    public float DeathTime = 60;

    /// <summary>
    /// If true, will prevent *almost* all types of revival.
    /// Right now, this just means it won't allow devils to revive.
    /// </summary>
    [DataField]
    public bool PreventAllRevives;

    /// <summary>
    /// How long it has been since the delayed death timer started.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float DeathTimer;

    /// <summary>
    /// What message is displayed when the time runs out.
    /// </summary>
    [DataField]
    public LocId DeathMessageId;

    /// <summary>
    /// What the defib displays when attempting to revive this entity.
    /// </summary>
    [DataField]
    public LocId DefibFailMessageId = "defibrillator-missing-organs";
}