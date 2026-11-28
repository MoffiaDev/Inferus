// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Traumas/TraumasSerializable.cs) // Inferus
// Inferus - BoneSeverity and OrganSeverity already exist next to their components. The two integrity events are new,
// the bone and organ systems raise them so traumas can clear when the part is fully healed.
using Content.Shared._Shitmed.Surgery.Organs;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Traumas;

[ByRefEvent]
public record struct ApplyTraumaEvent(
    ProtoId<TraumaTypePrototype> TraumaType,
    Entity<WoundableComponent> Woundable,
    Entity<TraumaInflicterComponent> Inflicter,
    EntityUid Target,
    FixedPoint2 Severity,
    bool Handled = false);

[ByRefEvent]
public record struct TraumaChanceDeductionEvent(FixedPoint2 TraumaSeverity, ProtoId<TraumaTypePrototype> TraumaType, FixedPoint2 ChanceDeduction);

[ByRefEvent]
public record struct BeforeTraumaInducedEvent(FixedPoint2 TraumaSeverity, EntityUid TraumaTarget, ProtoId<TraumaTypePrototype> TraumaType, bool Cancelled = false);

[ByRefEvent]
public record struct TraumaInducedEvent(Entity<TraumaComponent> Trauma, EntityUid TraumaTarget, FixedPoint2 TraumaSeverity, ProtoId<TraumaTypePrototype> TraumaType);

[ByRefEvent]
public record struct TraumaBeingRemovedEvent(Entity<TraumaComponent> Trauma, EntityUid TraumaTarget, FixedPoint2 TraumaSeverity, ProtoId<TraumaTypePrototype> TraumaType);

/// <summary>
/// Raised on a bone when its integrity changed.
/// </summary>
[ByRefEvent]
public record struct BoneIntegrityChangedEvent(Entity<BoneComponent> Bone, FixedPoint2 OldIntegrity, FixedPoint2 NewIntegrity);

/// <summary>
/// Raised on an internal organ when its integrity changed.
/// </summary>
[ByRefEvent]
public record struct OrganIntegrityChangedEvent(Entity<OrganDamageComponent> Organ, FixedPoint2 OldIntegrity, FixedPoint2 NewIntegrity);