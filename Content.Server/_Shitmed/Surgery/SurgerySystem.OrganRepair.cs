// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - mending bruised, damaged and failed organs.
using Content.Shared._Shitmed.Surgery.Operations;
using Content.Shared._Shitmed.Surgery.Organs;
using Content.Shared.Body;

namespace Content.Server._Shitmed.Surgery;

public sealed partial class SurgerySystem
{
    [Dependency] private OrganDamageSystem _organDamage = default!;

    private List<Entity<OrganDamageComponent>> GetHurtOrgans(EntityUid part)
    {
        var result = new List<Entity<OrganDamageComponent>>();
        foreach (var child in _organRelation.AllChildren(part))
        {
            if (child.Comp.Parent != part
                || !HasComp<InternalChildOrganComponent>(child.Owner)
                || !TryComp<OrganDamageComponent>(child.Owner, out var damage)
                || damage.Severity == OrganSeverity.Healthy)
                continue;

            result.Add((child.Owner, damage));
        }

        return result;
    }

    [SubscribeLocalEvent]
    private void OnTendOrgansStep(Entity<SurgeryTendOrgansStepComponent> ent, ref SurgeryStepEvent args)
    {
        foreach (var organ in GetHurtOrgans(args.Part))
        {
            _organDamage.Heal(organ, ent.Comp.Amount);
        }
    }

    [SubscribeLocalEvent]
    private void OnTendOrgansCheck(Entity<SurgeryTendOrgansStepComponent> ent, ref SurgeryStepCompleteCheckEvent args)
    {
        if (GetHurtOrgans(args.Part).Count > 0)
            args.Cancelled = true;
    }

    [SubscribeLocalEvent]
    private void OnOrgansDamagedCondition(Entity<SurgeryOrgansDamagedConditionComponent> ent, ref SurgeryValidEvent args)
    {
        if (GetHurtOrgans(args.Part).Count == 0)
            args.Cancelled = true;
    }
}
