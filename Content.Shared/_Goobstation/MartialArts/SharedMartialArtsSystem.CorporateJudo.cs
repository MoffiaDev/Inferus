// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Grab;
using Content.Shared._Goobstation.MartialArts;
using Content.Shared._Goobstation.GrabIntent;
using Content.Shared._Goobstation.MartialArts.Components;
using Content.Shared._Goobstation.MartialArts.Events;
using Content.Shared.Clothing;
using Content.Shared.Damage;
using Content.Shared.Damage.Events;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Events;
using Content.Shared.Standing;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee;
using Robust.Shared.Audio;

namespace Content.Shared._Goobstation.MartialArts;

public partial class SharedMartialArtsSystem
{
    private void InitializeCorporateJudo()
    {
        SubscribeLocalEvent<CanPerformComboComponent, JudoDiscombobulatePerformedEvent>(OnJudoDiscombobulate);
        SubscribeLocalEvent<CanPerformComboComponent, JudoEyePokePerformedEvent>(OnJudoEyePoke);
        SubscribeLocalEvent<CanPerformComboComponent, JudoThrowPerformedEvent>(OnJudoThrow);
        SubscribeLocalEvent<CanPerformComboComponent, JudoArmbarPerformedEvent>(OnJudoArmbar);
        SubscribeLocalEvent<CanPerformComboComponent, JudoWheelThrowPerformedEvent>(OnJudoWheelThrow);
        SubscribeLocalEvent<CanPerformComboComponent, JudoGoldenBlastPerformedEvent>(OnJudoGoldenBlast);

        SubscribeLocalEvent<GrantCorporateJudoComponent, ClothingGotEquippedEvent>(OnGrantCorporateJudo);
        SubscribeLocalEvent<GrantCorporateJudoComponent, ClothingGotUnequippedEvent>(OnRemoveCorporateJudo);

        SubscribeLocalEvent<ArmbarredComponent, StoodEvent>(OnArmbarredStood);
        SubscribeLocalEvent<ArmbarredComponent, PullStoppedMessage>(OnArmbarStopped);
    }

    #region Generic Methods

    private void OnGrantCorporateJudo(Entity<GrantCorporateJudoComponent> ent, ref ClothingGotEquippedEvent args)
    {
        if (!_netManager.IsServer)
            return;

        var user = args.Wearer;
        TryGrantMartialArt(user, ent.Comp);
    }

    private void OnRemoveCorporateJudo(Entity<GrantCorporateJudoComponent> ent, ref ClothingGotUnequippedEvent args)
    {
        var user = args.Wearer;
        if (!TryComp<MartialArtsKnowledgeComponent>(user, out var martialArtsKnowledge))
            return;

        if (martialArtsKnowledge.MartialArtsForm != MartialArtsForms.CorporateJudo)
            return;

        if (!TryComp<MeleeWeaponComponent>(args.Wearer, out var meleeWeaponComponent))
            return;

        var originalDamage = new DamageSpecifier();
        originalDamage.DamageDict[martialArtsKnowledge.OriginalFistDamageType]
            = FixedPoint2.New(martialArtsKnowledge.OriginalFistDamage);
        meleeWeaponComponent.Damage = originalDamage;

        RemComp<MartialArtsKnowledgeComponent>(user);
        RemComp<CanPerformComboComponent>(user);
    }

    #endregion

    #region Combo Methods

    private void OnJudoDiscombobulate(Entity<CanPerformComboComponent> ent, ref JudoDiscombobulatePerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _)
            || !TryComp(target, out StatusEffectsComponent? status))
            return;

        _movementMod.TryUpdateMovementSpeedModDuration(target, MartsGenericSlow, TimeSpan.FromSeconds(5), 0.5f, 0.5f);

        _stamina.TakeStaminaDamage(target, proto.StaminaDamage);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnJudoEyePoke(Entity<CanPerformComboComponent> ent, ref JudoEyePokePerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out _)
            || !TryComp(target, out StatusEffectsComponent? status))
            return;

        _newStatus.TryUpdateStatusEffectDuration(target, "StatusEffectBlindness", TimeSpan.FromSeconds(2)); // Inferus - blindness is a new status effect upstream

        _status.TryAddStatusEffect<BlurryVisionComponent>(target,
            "BlurryVision",
            TimeSpan.FromSeconds(5),
            true,
            status);

        DoDamage(ent, target, proto.DamageType, proto.ExtraDamage, out _);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnJudoThrow(Entity<CanPerformComboComponent> ent, ref JudoThrowPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed)
            || downed
            || !TryComp<PullableComponent>(target, out var pullable))
            return;

        var knockdownTime = proto.ParalyzeTime;

        var ev = new BeforeStaminaDamageEvent(1f);
        RaiseLocalEvent(target, ref ev);

        knockdownTime *= ev.Value;

        _stun.TryKnockdown(target, knockdownTime, true, true, proto.DropItems);

        _stamina.TakeStaminaDamage(target, proto.StaminaDamage);

        _pulling.TryStopPull(target, pullable, ent, true);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnJudoArmbar(Entity<CanPerformComboComponent> ent, ref JudoArmbarPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed)
            || !downed
            || !TryComp<PullerComponent>(ent, out var puller)
            || !TryComp<GrabIntentComponent>(ent, out var grabIntent)
            || !TryComp<PullableComponent>(target, out var pullable)
            || !TryComp<GrabbableComponent>(target, out var grabbable))
            return;

        var knockdownTime = proto.ParalyzeTime;

        var ev = new BeforeStaminaDamageEvent(1f);
        RaiseLocalEvent(target, ref ev);

        knockdownTime *= ev.Value;

        if (!HasComp<ArmbarredComponent>(target))
        {
            _stamina.TakeStaminaDamage(target, proto.StaminaDamage);
            AddComp<ArmbarredComponent>(target).Puller = ent;
        }

        // Taking someone in an armbar is an equivalent of taking them in a choke grab
        if (grabIntent.GrabStage != GrabStage.Suffocate
            || grabbable.GrabStage != GrabStage.Suffocate)
            _grab.TrySetGrabStages((ent, puller, grabIntent), (target, pullable, grabbable), GrabStage.Suffocate);

        _stun.TryKnockdown(target, knockdownTime, true, true, proto.DropItems);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    private void OnJudoWheelThrow(Entity<CanPerformComboComponent> ent, ref JudoWheelThrowPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var downed)
            || !downed
            || !TryComp<PullableComponent>(target, out var pullable)
            || !TryComp<ArmbarredComponent>(target, out var armbarred)
            || armbarred.Puller != ent.Owner)
            return;

        _stamina.TakeStaminaDamage(target, proto.StaminaDamage);

        _pulling.TryStopPull(target, pullable, ent, true);
        _grabThrowing.Throw(target,
            ent,
            _transform.GetMapCoordinates(ent).Position - _transform.GetMapCoordinates(target).Position,
            5,
            behavior: proto.DropItems);

        _stun.ForceStandUp(ent.Owner); // Inferus - knockdown is a component upstream, not a status effect

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    // Not implemented yet, but I'll leave it here
    private void OnJudoGoldenBlast(Entity<CanPerformComboComponent> ent, ref JudoGoldenBlastPerformedEvent args)
    {
        if (!_proto.TryIndex(ent.Comp.BeingPerformed, out var proto)
            || !TryUseMartialArt(ent, proto, out var target, out var _)
            || !TryComp(target, out StatusEffectsComponent? status)
            || !TryComp<PullableComponent>(target, out var pullable))
            return;

        _stun.TryUpdateParalyzeDuration(target, proto.ParalyzeTime);

        _pulling.TryStopPull(target, pullable, ent, true);

        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Weapons/genhit3.ogg"), target);
        ComboPopup(ent, target, proto.Name);
        ent.Comp.LastAttacks.Clear();
    }

    #endregion

    #region Armbar

    private void OnArmbarredStood(Entity<ArmbarredComponent> ent, ref StoodEvent args)
    {
        if (!TryComp<PullableComponent>(ent, out var pullable))
            return;

        _pulling.TryStopPull(ent, pullable, ent.Comp.Puller, true);
        RemComp<ArmbarredComponent>(ent);
    }

    private void OnArmbarStopped(Entity<ArmbarredComponent> ent, ref PullStoppedMessage args)
    {
        if (args.PullerUid != ent.Comp.Puller)
            return;

        if (!_newStatus.HasStatusEffect(ent, SharedStunSystem.StunId)) // Inferus - stun and knockdown are not old status effects upstream
            _stun.ForceStandUp(ent.Owner);

        RemComp<ArmbarredComponent>(ent);
    }

    #endregion
}
