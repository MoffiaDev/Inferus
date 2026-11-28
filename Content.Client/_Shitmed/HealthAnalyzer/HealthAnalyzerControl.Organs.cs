// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - lists the internal organs of the scanned patient that are hurt.
using Content.Shared._Shitmed.Surgery.Organs;
using Robust.Client.UserInterface.Controls;
using BodySystem = Content.Shared.Body.BodySystem;

namespace Content.Client.HealthAnalyzer.UI;

public sealed partial class HealthAnalyzerControl
{
    private void DrawOrganStatus(EntityUid target)
    {
        var body = _entityManager.System<BodySystem>();
        BoxContainer? container = null;

        foreach (var organ in body.EnumerateOrgans<OrganDamageComponent>(target))
        {
            var severity = organ.Comp2.Severity;
            if (severity == OrganSeverity.Healthy)
                continue;

            container ??= CreateOrganContainer();

            var name = _entityManager.GetComponent<MetaDataComponent>(organ.Owner).EntityName;
            container.AddChild(new Label
            {
                Text = $" · {name}: {Loc.GetString($"shitmed-organ-status-{severity.ToString().ToLowerInvariant()}")}",
                FontColorOverride = severity switch
                {
                    OrganSeverity.Bruised => Color.FromHex("#E6E36A"),
                    OrganSeverity.Damaged => Color.FromHex("#E8662F"),
                    _ => Color.FromHex("#A01818"),
                },
            });
        }

        if (container != null)
            GroupsContainer.AddChild(container);
    }

    private static BoxContainer CreateOrganContainer()
    {
        var container = new BoxContainer
        {
            Margin = new Thickness(0, 6, 0, 0),
            Orientation = BoxContainer.LayoutOrientation.Vertical,
        };
        container.AddChild(new Label { Text = Loc.GetString("health-analyzer-organs-title") });
        return container;
    }
}
