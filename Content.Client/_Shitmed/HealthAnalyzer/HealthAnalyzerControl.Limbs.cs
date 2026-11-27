// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - lists the wounded limbs of the scanned patient in the health analyzer.
using Content.Shared._Shitmed.Surgery.Bleeding;
using Content.Shared._Shitmed.Surgery.Pain;
using Content.Shared._Shitmed.Surgery.Traumas;
using Content.Shared._Shitmed.Surgery.Wounds;
using Content.Shared._Shitmed.Targeting;
using Content.Shared.Body;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using BodySystem = Content.Shared.Body.BodySystem;

namespace Content.Client.HealthAnalyzer.UI;

public sealed partial class HealthAnalyzerControl
{
    // Groin is left out, it shares its status with the chest (both come from the torso).
    private static readonly (TargetBodyPart Part, string Key)[] LimbRows =
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

    /// <summary>
    /// Adds a section listing every limb that is hurt, has a damaged bone or a tourniquet, and the patient's pain.
    /// Wounds come from the patient's replicated <see cref="TargetingComponent"/>, bones and tourniquets from the limb organs.
    /// </summary>
    private void DrawLimbStatus(EntityUid target)
    {
        if (!_entityManager.TryGetComponent(target, out TargetingComponent? targeting))
            return;

        var extras = new Dictionary<ProtoId<OrganCategoryPrototype>, (BoneSeverity Bone, bool Tourniquet)>();
        var body = _entityManager.System<BodySystem>();
        foreach (var limb in body.EnumerateOrgans<BoneComponent>(target))
        {
            if (limb.Comp1.Category is not { } category)
                continue;

            extras[category] = (limb.Comp2.BoneSeverity, _entityManager.HasComponent<TourniquetedComponent>(limb.Owner));
        }

        BoxContainer? container = null;

        foreach (var (part, key) in LimbRows)
        {
            if (!targeting.BodyStatus.TryGetValue(part, out var severity))
                severity = WoundableSeverity.Healthy;

            extras.TryGetValue(SharedTargetingSystem.ToOrganCategory(part), out var extra);

            if (severity == WoundableSeverity.Healthy && extra.Bone == BoneSeverity.Normal && !extra.Tourniquet)
                continue;

            var parts = new List<string>();
            if (severity != WoundableSeverity.Healthy)
                parts.Add(Loc.GetString(SeverityKey(severity)));

            if (extra.Bone != BoneSeverity.Normal)
                parts.Add(Loc.GetString($"shitmed-part-status-bone-{extra.Bone.ToString().ToLowerInvariant()}"));

            if (extra.Tourniquet)
                parts.Add(Loc.GetString("shitmed-part-status-tourniquet"));

            container ??= CreateLimbContainer();
            container.AddChild(new Label
            {
                Text = $" · {Loc.GetString(key)}: {string.Join(", ", parts)}",
                FontColorOverride = severity == WoundableSeverity.Healthy ? Color.FromHex("#F2A93B") : SeverityColor(severity),
            });
        }

        if (_entityManager.TryGetComponent(target, out PainComponent? pain) && pain.Level != PainLevel.None)
        {
            container ??= CreateLimbContainer();
            container.AddChild(new Label
            {
                Text = Loc.GetString($"health-analyzer-pain-{pain.Level.ToString().ToLowerInvariant()}"),
                FontColorOverride = Color.FromHex("#E8662F"),
            });
        }

        if (container != null)
            GroupsContainer.AddChild(container);
    }

    private static BoxContainer CreateLimbContainer()
    {
        var container = new BoxContainer
        {
            Margin = new Thickness(0, 6, 0, 0),
            Orientation = BoxContainer.LayoutOrientation.Vertical,
        };
        container.AddChild(new Label { Text = Loc.GetString("health-analyzer-limbs-title") });
        return container;
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
            _ => "health-analyzer-severity-healthy",
        };
    }

    private static Color SeverityColor(WoundableSeverity severity)
    {
        return severity switch
        {
            WoundableSeverity.Minor => Color.FromHex("#E6E36A"),
            WoundableSeverity.Moderate => Color.FromHex("#F2A93B"),
            WoundableSeverity.Severe => Color.FromHex("#E8662F"),
            WoundableSeverity.Critical => Color.FromHex("#D12B2B"),
            WoundableSeverity.Mangled => Color.FromHex("#A01818"),
            WoundableSeverity.Severed => Color.FromHex("#8A8A8A"),
            _ => Color.White,
        };
    }
}
