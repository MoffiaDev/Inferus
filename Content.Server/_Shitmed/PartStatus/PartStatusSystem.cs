// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Server/_Shitmed/PartStatus/PartStatusSystem.cs) // Inferus
// Inferus - the report lists every limb with its wound and bone state, and your pain. Per-wound bleeding is not part of it yet.
using Content.Server.Chat.Managers;
using Content.Shared._Shitmed.PartStatus;
using Content.Shared._Shitmed.Surgery.Bleeding;
using Content.Shared._Shitmed.Surgery.Pain;
using Content.Shared._Shitmed.Surgery.Traumas;
using Content.Shared._Shitmed.Surgery.Wounds;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body;
using Content.Shared.Chat;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server._Shitmed.PartStatus;

public sealed class PartStatusSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private LimbBleedingSystem _bleeding = default!;

    private static readonly (TargetBodyPart Part, string Key)[] Rows =
    {
        (TargetBodyPart.Head, "health-analyzer-limb-head"),
        (TargetBodyPart.Chest, "health-analyzer-limb-torso"),
        (TargetBodyPart.LeftArm, "health-analyzer-limb-left-arm"),
        (TargetBodyPart.LeftHand, "health-analyzer-limb-left-hand"),
        (TargetBodyPart.RightArm, "health-analyzer-limb-right-arm"),
        (TargetBodyPart.RightHand, "health-analyzer-limb-right-hand"),
        (TargetBodyPart.LeftLeg, "health-analyzer-limb-left-leg"),
        (TargetBodyPart.LeftFoot, "health-analyzer-limb-left-foot"),
        (TargetBodyPart.RightLeg, "health-analyzer-limb-right-leg"),
        (TargetBodyPart.RightFoot, "health-analyzer-limb-right-foot"),
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<GetPartStatusEvent>(OnGetPartStatus);
    }

    private void OnGetPartStatus(GetPartStatusEvent message, EntitySessionEventArgs args)
    {
        var body = GetEntity(message.Uid);

        // A player can only check themselves, and only while able to.
        if (args.SenderSession.AttachedEntity != body || _mobState.IsIncapacitated(body))
            return;

        var limbs = new Dictionary<ProtoId<OrganCategoryPrototype>, (WoundableSeverity Wounds, BoneSeverity Bone, bool Bleeding, bool Tourniquet)>();
        foreach (var limb in _body.EnumerateOrgans<WoundableComponent>(body))
        {
            if (limb.Comp1.Category is not { } category)
                continue;

            var bone = TryComp<BoneComponent>(limb, out var boneComp) ? boneComp.BoneSeverity : BoneSeverity.Normal;
            limbs[category] = (limb.Comp2.WoundableSeverity, bone, _bleeding.IsBleeding(limb.Owner, limb.Comp2), HasComp<TourniquetedComponent>(limb));
        }

        var lines = new List<string> { Loc.GetString("shitmed-part-status-title") };

        foreach (var (part, key) in Rows)
        {
            var name = Loc.GetString(key);
            string status;

            if (!limbs.TryGetValue(SharedTargetingSystem.ToOrganCategory(part), out var state))
            {
                status = Loc.GetString("health-analyzer-severity-severed");
            }
            else
            {
                var parts = new List<string>();
                if (state.Wounds != WoundableSeverity.Healthy)
                    parts.Add(Loc.GetString(SeverityKey(state.Wounds)));

                if (state.Bone != BoneSeverity.Normal)
                    parts.Add(Loc.GetString($"shitmed-part-status-bone-{state.Bone.ToString().ToLowerInvariant()}"));

                if (state.Tourniquet)
                    parts.Add(Loc.GetString("shitmed-part-status-tourniquet"));
                else if (state.Bleeding)
                    parts.Add(Loc.GetString("shitmed-part-status-bleeding"));

                status = parts.Count == 0
                    ? Loc.GetString("shitmed-part-status-fine")
                    : string.Join(", ", parts);
            }

            lines.Add("    " + Loc.GetString("shitmed-part-status-line", ("part", name), ("status", status)));
        }

        if (TryComp<PainComponent>(body, out var pain) && pain.Level != PainLevel.None)
            lines.Add(Loc.GetString($"shitmed-part-status-pain-{pain.Level.ToString().ToLowerInvariant()}"));

        var text = string.Join("\n", lines);
        _chat.ChatMessageToOne(
            ChatChannel.Emotes,
            text,
            text,
            EntityUid.Invalid,
            false,
            args.SenderSession.Channel,
            recordReplay: false);
    }

    private static string SeverityKey(WoundableSeverity severity)
    {
        return severity switch
        {
            WoundableSeverity.Minor => "health-analyzer-severity-minor",
            WoundableSeverity.Moderate => "health-analyzer-severity-moderate",
            WoundableSeverity.Severe => "health-analyzer-severity-severe",
            WoundableSeverity.Critical => "health-analyzer-severity-critical",
            WoundableSeverity.Mangled => "health-analyzer-severity-mangled",
            WoundableSeverity.Severed => "health-analyzer-severity-severed",
            _ => "shitmed-part-status-fine",
        };
    }
}
