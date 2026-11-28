// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Traumas/Systems/TraumaSystem.Bones.cs) // Inferus
// Inferus - the odds come from the worst arm or hand bone of the body, not from the hand that holds the weapon.
// It is decided on the server, so a fumbled attack is simply cancelled there.
using Content.Shared._Shitmed.Surgery.Traumas;
using Content.Shared._Shitmed.Surgery.Traumas.Systems;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;
using Content.Shared.Popups;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server._Shitmed.Surgery.Traumas;

public sealed class BoneFumbleSystem : EntitySystem
{
    private static readonly SoundPathSpecifier FumbleSound = new("/Audio/Effects/slip.ogg");

    [Dependency] private BodySystem _body = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeleeWeaponComponent, AttemptMeleeEvent>(OnMeleeAttempt);
        SubscribeLocalEvent<GunComponent, ShotAttemptedEvent>(OnShotAttempt);
    }

    private void OnMeleeAttempt(Entity<MeleeWeaponComponent> weapon, ref AttemptMeleeEvent args)
    {
        if (!args.Cancelled && TryFumble(args.User))
            args.Cancelled = true;
    }

    private void OnShotAttempt(Entity<GunComponent> gun, ref ShotAttemptedEvent args)
    {
        if (TryFumble(args.User))
            args.Cancel();
    }

    private bool TryFumble(EntityUid user)
    {
        if (!HasComp<WoundDamageModelComponent>(user))
            return false;

        var odds = 0f;
        foreach (var limb in _body.EnumerateOrgans<BoneComponent>(user))
        {
            if (limb.Comp1.Category is not { } category || !BoneDoAfterSystem.ArmCategories.Contains(category.Id))
                continue;

            var limbOdds = limb.Comp2.BoneSeverity switch
            {
                BoneSeverity.Cracked => 0.10f,
                BoneSeverity.Broken => 0.25f,
                _ => 0f,
            };

            odds = Math.Max(odds, limbOdds);
        }

        if (odds <= 0f || !_random.Prob(odds))
            return false;

        _popup.PopupEntity(Loc.GetString("trauma-arm-fumble"), user, user, PopupType.SmallCaution);
        _audio.PlayPvs(FumbleSound, user);
        return true;
    }
}