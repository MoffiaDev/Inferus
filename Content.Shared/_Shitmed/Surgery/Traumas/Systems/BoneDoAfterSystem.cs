// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Traumas/Systems/TraumaSystem.Bones.cs) // Inferus
// Inferus - the bones of the arms and hands slow every do-after of the body, like Goob does for every bone.
using Content.Shared._Shitmed.DoAfter;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;

namespace Content.Shared._Shitmed.Surgery.Traumas.Systems;

public sealed class BoneDoAfterSystem : EntitySystem
{
    /// <summary>
    /// The limbs that matter when the body works with its hands.
    /// </summary>
    public static readonly HashSet<string> ArmCategories = ["ArmLeft", "ArmRight", "HandLeft", "HandRight"];

    [Dependency] private BodySystem _body = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WoundDamageModelComponent, GetDoAfterDelayMultiplierEvent>(OnGetMultiplier);
    }

    private void OnGetMultiplier(EntityUid uid, WoundDamageModelComponent comp, GetDoAfterDelayMultiplierEvent args)
    {
        foreach (var limb in _body.EnumerateOrgans<BoneComponent>(uid))
        {
            if (limb.Comp1.Category is not { } category || !ArmCategories.Contains(category.Id))
                continue;

            args.Multiplier *= limb.Comp2.BoneSeverity switch
            {
                BoneSeverity.Damaged => 1.09f,
                BoneSeverity.Cracked => 1.19f,
                BoneSeverity.Broken => 1.33f,
                _ => 1f,
            };
        }
    }
}