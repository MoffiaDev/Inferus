// Goobstation / Lavaland: action helpers used by megafauna AI.
// Ported from Goob Station (https://github.com/Goob-Station/Goob-Station), AGPL-3.0-or-later.
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared.Actions;

public abstract partial class SharedActionsSystem
{
    /// <summary>
    /// Finds an action granted to <paramref name="actionContainer"/> by its entity prototype id.
    /// </summary>
    public bool TryGetActionById(
        EntityUid actionContainer,
        EntProtoId actionId,
        [NotNullWhen(true)] out Entity<ActionComponent>? action)
    {
        action = null;
        var actions = GetActions(actionContainer).ToList();
        foreach (var (uid, comp) in actions)
        {
            if (TerminatingOrDeleted(uid))
                continue;

            var entityPrototypeId = MetaData(uid).EntityPrototype?.ID;
            if (entityPrototypeId == null
                || entityPrototypeId != actionId)
                continue;

            action = (uid, comp);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Runs all checks to see if user currently can perform some action, without performing it.
    /// </summary>
    public bool CanPerformAction(Entity<ActionsComponent?> user, Entity<ActionComponent> action, RequestPerformActionEvent ev)
    {
        if (!Resolve(user.Owner, ref user.Comp, false))
            return false;

        // Does the user actually have the requested action?
        if (!user.Comp.Actions.Contains(action))
            return false;

        if (!action.Comp.Enabled)
            return false;

        if (IsCooldownActive(action, GameTiming.CurTime))
            return false;

        // check for action use prevention
        var attemptEv = new ActionAttemptEvent(user, GetEntity(ev.EntityTarget));
        RaiseLocalEvent(action, ref attemptEv);
        if (attemptEv.Cancelled)
            return false;

        // Validate request by checking action blockers and the like
        var provider = action.Comp.Container ?? user;
        var validateEv = new ActionValidateEvent()
        {
            Input = ev,
            User = user,
            Provider = provider
        };
        RaiseLocalEvent(action, ref validateEv);
        return !validateEv.Invalid;
    }
}
