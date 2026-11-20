// SPDX-License-Identifier: MIT

using Content.Server._Omu.Crewlet;
using Content.Server._Omu.StationEvents.Components;
using Content.Server.Mind;
using Content.Server.Roles;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Omu.StationEvents.Events;

public sealed partial class CrewletOmenRule : StationEventSystem<CrewletOmenRuleComponent> // Inferus
{
    [Dependency] private IRobustRandom _random = default!; // Inferus
    [Dependency] private CrewletGestationSystem _gestation = default!; // Inferus
    [Dependency] private CrewletSystem _crewlet = default!; // Inferus
    [Dependency] private MindSystem _mind = default!; // Inferus
    [Dependency] private RoleSystem _role = default!; // Inferus

    private readonly List<EntityUid> _candidates = new();

    protected override void Started(EntityUid uid,
        CrewletOmenRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station))
            return;

        _candidates.Clear();
        using var query = EntityQueryEnumerator<ActorComponent, HumanoidProfileComponent, MobStateComponent,
            TransformComponent>();
        while (query.MoveNext(out var target, out _, out _, out var mobState, out var xform))
        {
            if (mobState.CurrentState != MobState.Alive
                || !_crewlet.CanCreate(target)
                || StationSystem.GetOwningStation(target, xform) != station
                || !_mind.TryGetMind(target, out var mind, out _)
                || _role.MindIsAntagonist(mind))
                continue;

            _candidates.Add(target);
        }

        if (_candidates.Count > 0)
        {
            var host = _random.Pick(_candidates);
            _gestation.Seed(host, host);
        }

        _candidates.Clear();
    }
}
