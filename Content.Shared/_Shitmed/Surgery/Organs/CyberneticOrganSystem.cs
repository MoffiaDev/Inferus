// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - what an EMP does to cybernetic organs. Organs inside a body are not found by the pulse, only the body is, so the body
// passes the pulse on to every machine organ it carries. A disabled organ counts as lost to the body, like a failed one, until the
// EMP wears off.
using System.Linq;
using Content.Shared._Shitmed.Surgery.Wounds.Systems;
using Content.Shared.Body;
using Content.Shared.Emp;
using Robust.Shared.Network;

namespace Content.Shared._Shitmed.Surgery.Organs;

public sealed partial class CyberneticOrganSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private BodySystem _body = default!;
    [Dependency] private OrganDamageSystem _organDamage = default!;
    [Dependency] private SharedEmpSystem _emp = default!;
    [Dependency] private WoundSystem _wounds = default!;

    /// <summary>
    /// The pulse hits the body, which hands it on to its machine organs.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnBodyEmp(Entity<BodyComponent> body, ref EmpPulseEvent args)
    {
        foreach (var organ in _body.EnumerateOrgans<CyberneticOrganComponent>(body.Owner).ToList())
        {
            _emp.DoEmpEffects(organ.Owner, args.EnergyConsumption, args.Duration, args.User);
        }
    }

    [SubscribeLocalEvent]
    private void OnOrganEmp(Entity<CyberneticOrganComponent> ent, ref EmpPulseEvent args)
    {
        args.Affected = true;
        args.Disabled = true;

        if (_net.IsClient)
            return;

        if (TryComp<OrganDamageComponent>(ent, out var damage))
            _organDamage.ApplyDamage((ent.Owner, damage), ent.Comp.EmpDamage);

        if (TryComp<OrganComponent>(ent, out var organ) && organ.Body is { } body && organ.Category is { } category)
            _wounds.SetOrganLost(body, category, true);
    }

    /// <summary>
    /// The EMP is over, the organ works again unless it has failed in the meantime.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnEmpOver(Entity<CyberneticOrganComponent> ent, ref EmpDisabledRemovedEvent args)
    {
        if (_net.IsClient
            || !TryComp<OrganComponent>(ent, out var organ)
            || organ.Body is not { } body
            || organ.Category is not { } category
            || TryComp<OrganDamageComponent>(ent, out var damage) && damage.Severity == OrganSeverity.Failed)
            return;

        _wounds.SetOrganLost(body, category, false);
    }
}
