// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (Content.Server/_Starlight/HoloItem/HoloItemSystem.cs) // Inferus
using Content.Shared._Shitmed.HoloItem;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.PowerCell;

namespace Content.Server._Shitmed.HoloItem;

public sealed partial class HoloItemSystem : EntitySystem
{
    [Dependency] private PowerCellSystem _powerCell = default!;
    [Dependency] private IComponentFactory _componentFactory = default!;

    [SubscribeLocalEvent]
    private void OnExamine(Entity<HoloItemComponent> ent, ref ExaminedEvent args)
    {
        // The same as the holosigns do
        var charges = _powerCell.GetRemainingUses(ent.Owner, ent.Comp.ChargeUse);
        var maxCharges = _powerCell.GetMaxUses(ent.Owner, ent.Comp.ChargeUse);

        using (args.PushGroup(nameof(HoloItemComponent)))
        {
            args.PushMarkup(Loc.GetString("limited-charges-charges-remaining", ("charges", charges)));

            if (charges > 0 && charges == maxCharges)
                args.PushMarkup(Loc.GetString("limited-charges-max-charges"));
        }
    }

    [SubscribeLocalEvent]
    private void OnInteract(Entity<HoloItemComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || ent.Comp.ItemPrototype is not { } proto)
            return;

        if (ent.Comp.UseOnTarget)
        {
            if (args.Target is not { } target)
                return;

            foreach (var name in ent.Comp.RequiredComponents)
            {
                if (!_componentFactory.TryGetRegistration(name, out var registration))
                {
                    Log.Error($"Unknown component name {name} in the required components of {ToPrettyString(ent)}.");
                    return;
                }

                if (!HasComp(target, registration.Type))
                    return;
            }

            if (!_powerCell.TryUseCharge(ent.Owner, ent.Comp.ChargeUse, user: args.User))
                return;

            var hologram = SpawnAtPosition(proto, Transform(ent).Coordinates);

            // Inferus - the hologram goes away by itself, and when the cuffs come off
+            EnsureComp<TemporaryHologramComponent>(hologram);

            var ev = new AfterInteractEvent(args.User, hologram, target, args.ClickLocation, true);
            RaiseLocalEvent(hologram, ev);
        }
        else
        {
            if (!_powerCell.TryUseCharge(ent.Owner, ent.Comp.ChargeUse, user: args.User))
                return;

            Spawn(proto, args.ClickLocation);
        }

        args.Handled = true;
    }
}
