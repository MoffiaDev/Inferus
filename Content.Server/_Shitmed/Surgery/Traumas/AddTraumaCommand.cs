// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Server/_Shitmed/Medical/Trauma/AddTraumaCommand.cs) // Inferus
// Inferus - the part is an organ category like ArmLeft or Torso.
using System.Linq;
using Content.Server.Administration;
using Content.Shared._Shitmed.Surgery.Traumas;
using Content.Shared._Shitmed.Surgery.Traumas.Systems;
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Administration;
using Content.Shared.Body;
using Content.Shared.FixedPoint;
using Robust.Shared.Console;
using Robust.Shared.Prototypes;

namespace Content.Server._Shitmed.Surgery.Traumas;

[AdminCommand(AdminFlags.Debug)]
public sealed class AddTraumaCommand : IConsoleCommand
{
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    public string Command => "addtrauma";
    public string Description => "Inflicts a trauma on a body, on a limb of the given category if there is one.";
    public string Help => "Usage: addtrauma <target> <traumaType> [limbCategory] [severity]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length is < 2 or > 4)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        if (!NetEntity.TryParse(args[0], out var targetNet) || !_entManager.TryGetEntity(targetNet, out var target))
        {
            shell.WriteError(Loc.GetString("shell-entity-uid-must-be-number"));
            return;
        }

        if (!_proto.HasIndex<TraumaTypePrototype>(args[1]))
        {
            shell.WriteError($"No trauma type prototype '{args[1]}'. Known: {string.Join(", ", _proto.EnumeratePrototypes<TraumaTypePrototype>().Select(p => p.ID))}.");
            return;
        }

        var traumaType = new ProtoId<TraumaTypePrototype>(args[1]);
        var categoryFilter = args.Length >= 3 ? args[2] : null;

        var severity = FixedPoint2.New(12);
        if (args.Length >= 4)
        {
            if (!float.TryParse(args[3], out var parsed))
            {
                shell.WriteError($"Severity '{args[3]}' is not a number.");
                return;
            }

            severity = FixedPoint2.New(parsed);
        }

        var body = _entManager.System<BodySystem>();
        var trauma = _entManager.System<TraumaSystem>();

        Entity<WoundableComponent>? chosen = null;
        foreach (var limb in body.EnumerateOrgans<WoundableComponent>(target.Value))
        {
            if (categoryFilter != null && !string.Equals(limb.Comp1.Category?.Id, categoryFilter, StringComparison.OrdinalIgnoreCase))
                continue;

            chosen = (limb.Owner, limb.Comp2);
            break;
        }

        if (chosen == null)
        {
            shell.WriteError(categoryFilter != null
                ? $"No woundable {categoryFilter} found on {_entManager.ToPrettyString(target.Value)}."
                : $"No woundable limbs found on {_entManager.ToPrettyString(target.Value)}.");
            return;
        }

        if (trauma.TryInflictTrauma(chosen.Value, traumaType, severity))
            shell.WriteLine($"Inflicted {traumaType} on {_entManager.ToPrettyString(chosen.Value.Owner)}.");
        else
            shell.WriteError($"Failed to inflict {traumaType}. The limb must be attached to a body.");
    }
}