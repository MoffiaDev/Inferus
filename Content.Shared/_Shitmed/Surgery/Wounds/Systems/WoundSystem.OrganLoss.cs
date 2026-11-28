// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - lets other systems mark an organ category as lost or back, for organs that fail without being taken out.
using Content.Shared._Shitmed.Surgery.Organs;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared._Shitmed.Surgery.Wounds.Systems;

public sealed partial class WoundSystem
{
    public void SetOrganLost(EntityUid body, ProtoId<OrganCategoryPrototype> category, bool lost)
    {
        if (TerminatingOrDeleted(body) || !TryComp<WoundDamageModelComponent>(body, out var model))
            return;

        LostOrgansComponent? comp;
        if (lost)
        {
            comp = EnsureComp<LostOrgansComponent>(body);
            if (!comp.Categories.Add(category))
                return;
        }
        else if (!TryComp(body, out comp) || !comp.Categories.Remove(category))
        {
            return;
        }

        Dirty(body, comp);
        OnLostOrgansChanged((body, model));
    }

    /// <summary>
    /// A failed organ stays lost when it is put into a body, until it is repaired.
    /// </summary>
    private bool IsOrganFailed(EntityUid organ)
    {
        return TryComp<OrganDamageComponent>(organ, out var damage) && damage.Severity == OrganSeverity.Failed;
    }
}
