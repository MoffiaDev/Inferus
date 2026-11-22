// SPDX-License-Identifier: MIT

using Content.Server.NPC;
using Content.Server.NPC.HTN;
using Content.Server.NPC.HTN.PrimitiveTasks;

namespace Content.Server._Omu.Crewlet;

public sealed partial class CrewletBrainOperator : HTNOperator
{
    [Dependency] private CrewletBrainSystem _brain = default!;

    public override void Startup(NPCBlackboard blackboard)
    {
        base.Startup(blackboard);
        _brain.StartThinking(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
    }

    public override HTNOperatorStatus Update(NPCBlackboard blackboard, float frameTime)
    {
        _brain.Think(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
        return HTNOperatorStatus.Continuing;
    }

    public override void TaskShutdown(NPCBlackboard blackboard, HTNOperatorStatus status)
    {
        base.TaskShutdown(blackboard, status);
        _brain.StopThinking(blackboard.GetValue<EntityUid>(NPCBlackboard.Owner));
    }
}
