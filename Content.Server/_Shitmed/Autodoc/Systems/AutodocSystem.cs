// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Shitmed.Surgery;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Shared._Goobstation.Factory;
using Content.Shared._Shitmed.Autodoc.Components;
using Content.Shared._Shitmed.Autodoc.Systems;
using Content.Shared.Body.Components;
using Content.Shared.Chat;
using Content.Shared.DoAfter;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server._Shitmed.Autodoc.Systems;

public sealed class AutodocSystem : SharedAutodocSystem
{
    [Dependency] private InternalsSystem _internals = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SharedPowerReceiverSystem _power = default!;
    [Dependency] private SurgerySystem _surgery = default!;

    public override void Initialize()
    {
        base.Initialize();

        // raised because the autodoc prototype sets raiseEndedEvent on its DoAfter
        SubscribeLocalEvent<ActiveAutodocComponent, DoAfterEndedEvent>(OnDoAfterEnded);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveAutodocComponent, AutodocComponent>();
        var now = Timing.CurTime;
        while (query.MoveNext(out var uid, out var active, out var comp))
        {
            if (now < active.NextUpdate)
                continue;

            active.NextUpdate = now + comp.UpdateDelay;
            if (HasComp<ActiveDoAfterComponent>(uid) || !_power.IsPowered(uid))
                continue;

            if (Proceed((uid, comp, active)))
                RemCompDeferred<ActiveAutodocComponent>(uid);
        }
    }

    private void OnDoAfterEnded(Entity<ActiveAutodocComponent> ent, ref DoAfterEndedEvent args)
    {
        if (!TryComp<AutodocComponent>(ent, out var comp))
            return;

        if (!args.Cancelled)
        {
            ent.Comp.Waiting = false; // try the next autodoc or surgery step
            return;
        }

        var program = comp.Programs[ent.Comp.CurrentProgram];
        var error = Loc.GetString("autodoc-error-surgery-failed");
        if (program.SkipFailed)
        {
            Say(ent, Loc.GetString("autodoc-error", ("error", error)));
            ent.Comp.CurrentSurgery = null;
            ent.Comp.Waiting = false;
            ent.Comp.ProgramStep++;
        }
        else
        {
            Say(ent, Loc.GetString("autodoc-fatal-error", ("error", error)));
            RemCompDeferred<ActiveAutodocComponent>(ent);
        }
    }

    public override bool IsSurgery(EntProtoId id)
    {
        return _surgery.IsSurgery(id);
    }

    public override bool StartSurgeryOrThrow(Entity<AutodocComponent> ent, EntityUid patient, EntityUid part, EntProtoId surgery)
    {
        if (ent.Comp.RequireSleeping && IsAwake(patient))
            throw new AutodocError("patient-unsedated");

        switch (_surgery.TryStartStepFor(ent, patient, part, surgery, out var error))
        {
            case AutodocStepResult.Done:
                return false;
            case AutodocStepResult.Started:
                if (TryComp<ActiveAutodocComponent>(ent, out var active))
                {
                    active.CurrentSurgery = (patient, part, surgery);
                    active.Waiting = true; // don't go onto next step until the doafter finishes
                }
                return true;
            default:
                throw new AutodocError(error);
        }
    }

    protected override void WakePatient(EntityUid patient)
    {
        // incase they are using nitrous, disconnect it so they can get woken up later on
        if (TryComp<InternalsComponent>(patient, out var internals) && _internals.AreInternalsWorking(patient, internals))
            _internals.DisconnectTank((patient, internals));

        base.WakePatient(patient);
    }

    public override void Say(EntityUid uid, string msg)
    {
        _chat.TrySendInGameICMessage(uid, msg, InGameICChatType.Speak, hideChat: false, hideLog: true, checkRadioPrefix: false);
    }
}