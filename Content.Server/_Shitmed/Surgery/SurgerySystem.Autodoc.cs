// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - lets a machine with hands (the autodoc) run the next step of a surgery without a player clicking in the window.
using Content.Shared._Shitmed.Surgery.Operations;
using Content.Shared.DoAfter;
using Robust.Shared.Prototypes;

namespace Content.Server._Shitmed.Surgery;

public enum AutodocStepResult : byte
{
    Started,
    Done,
    Failed
}

public sealed partial class SurgerySystem
{
    public bool IsSurgery(EntProtoId id)
    {
        return _surgeries.Contains(id);
    }

    /// <summary>
    /// Starts the next step of a surgery for an entity with hands. Any held item that matches the tool of the step is used.
    /// Done means every step is already complete. Failed sets error to the end of an autodoc-error locale id.
    /// </summary>
    public AutodocStepResult TryStartStepFor(EntityUid user, EntityUid body, EntityUid part, EntProtoId surgeryId, out string error)
    {
        error = "surgery-impossible";

        if (!IsSurgeryValid(body, part, surgeryId, out var surgery))
            return AutodocStepResult.Failed;

        var next = GetNextStep(body, part, surgery);
        if (next < 0)
            return AutodocStepResult.Done;

        var stepId = surgery.Comp.Steps[next];
        if (GetSingleton(stepId) is not { } stepEnt || !TryComp<SurgeryStepComponent>(stepEnt, out var step))
            return AutodocStepResult.Failed;

        var validEv = new SurgeryValidEvent(body, part);
        RaiseLocalEvent(stepEnt, ref validEv);
        if (validEv.Cancelled)
            return AutodocStepResult.Failed;

        EntityUid? tool = null;
        ISurgeryToolComponent? matched = null;
        if (step.Tool == null)
        {
            tool = user;
        }
        else
        {
            foreach (var held in _hands.EnumerateHeld(user))
            {
                if (CheckTool(user, held, step, out matched, popup: false))
                {
                    tool = held;
                    break;
                }
            }
        }

        if (tool is not { } chosen)
        {
            error = "missing-tool";
            return AutodocStepResult.Failed;
        }

        var preCheck = new SurgeryStepPreCheckEvent(user, body, part, chosen);
        RaiseLocalEvent(stepEnt, ref preCheck);
        if (preCheck.Cancelled)
        {
            error = "step-invalid";
            return AutodocStepResult.Failed;
        }

        var duration = step.Duration / Math.Max(0.1f, matched?.Speed ?? 1f) / GetTableSpeed(body);
        var ev = new SurgeryDoAfterEvent(surgeryId, stepId, GetNetEntity(part));
        var doAfter = new DoAfterArgs(EntityManager, user, duration, ev, body, target: body, used: chosen == user ? null : chosen)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnWeightlessMove = false,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
        {
            error = "step-invalid";
            return AutodocStepResult.Failed;
        }

        return AutodocStepResult.Started;
    }
}