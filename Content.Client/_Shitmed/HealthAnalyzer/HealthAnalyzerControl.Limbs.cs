// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - lists the wounded limbs of the scanned patient in the health analyzer.
using Content.Shared._Shitmed.Surgery.Wounds;
using Content.Shared._Shitmed.Targeting;
using Robust.Client.UserInterface.Controls;

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
    /// Adds a section listing every limb that is not healthy, read from the patient's replicated <see cref="TargetingComponent"/>.
    /// </summary>
    private void DrawLimbStatus(EntityUid target)
    {
        if (!_entityManager.TryGetComponent(target, out TargetingComponent? targeting))
            return;

        BoxContainer? container = null;

        foreach (var (part, key) in LimbRows)
        {
            if (!targeting.BodyStatus.TryGetValue(part, out var severity) || severity == WoundableSeverity.Healthy)
                continue;

            if (container == null)
            {
                container = new BoxContainer
                {
                    Margin = new Thickness(0, 6, 0, 0),
                    Orientation = BoxContainer.LayoutOrientation.Vertical,
                };
                container.AddChild(new Label { Text = Loc.GetString("health-analyzer-limbs-title") });
            }

            container.AddChild(new Label
            {
                Text = $" · {Loc.GetString(key)}: {Loc.GetString(SeverityKey(severity))}",
                FontColorOverride = SeverityColor(severity),
            });
        }

        if (container != null)
            GroupsContainer.AddChild(container);
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
