using Content.Server.Radio.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Trigger;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.Map; // Lavaland Change
using Robust.Shared.Utility;

namespace Content.Server.Trigger.Systems;

public sealed partial class RattleOnTriggerSystem : EntitySystem
{
    [Dependency] private RadioSystem _radio = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private SharedTransformSystem _transform = default!; // Lavaland Change

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RattleOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<RattleOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        if (!TryComp<MobStateComponent>(target.Value, out var mobstate))
            return;

        args.Handled = true;

        if (!ent.Comp.Messages.TryGetValue(mobstate.CurrentState, out var messageId))
            return;

        // Gets the location of the user
        // Lavaland Change start - added ReportCoordinates check
        string posText;
        if (ent.Comp.ReportCoordinates)
        {
            posText = "Unknown";
            var mapCoords = _transform.GetMapCoordinates(target.Value);
            if (mapCoords.MapId != MapId.Nullspace)
                posText = $"({(int) mapCoords.Position.X}, {(int) mapCoords.Position.Y})";
        }
        else
            posText = FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString(target.Value));
        // Lavaland Change end

        var message = Loc.GetString(messageId, ("user", target.Value), ("position", posText));
        // Sends a message to the radio channel specified by the implant
        _radio.SendRadioMessage(ent.Owner, message, ProtoMan.Index(ent.Comp.RadioChannel), ent.Owner);
    }
}
