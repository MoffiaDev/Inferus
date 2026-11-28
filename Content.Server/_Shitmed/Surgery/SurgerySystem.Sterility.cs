// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - any surgical step poisons the patient when nobody around them wears a sterile mask and sterile gloves.
// Wearing the gear is checked in SurgerySystem.Effects.cs (HasPpe).
using Content.Shared._Shitmed.Surgery.Operations;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.Inventory;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server._Shitmed.Surgery;

public sealed partial class SurgerySystem
{
    /// <summary>
    /// The toxin damage every unsterile step does.
    /// </summary>
    private const float UnsterileToxin = 6f;

    [Dependency] private DamageableSystem _damageable = default!;

    /// <summary>
    /// Whether the surgeon, or anyone near the patient, wears both a sterile mask and sterile gloves.
    /// </summary>
    private bool AnyonePpe(EntityUid user, EntityUid body)
    {
        if (HasPpe(user))
            return true;

        var bodyCoordinates = Transform(body).Coordinates;
        var query = EntityQueryEnumerator<ActorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out _, out var xform))
        {
            if (uid == body || uid == user || !HasComp<InventoryComponent>(uid))
                continue;

            if (_transform.InRange(xform.Coordinates, bodyCoordinates, SterileRange) && HasPpe(uid))
                return true;
        }

        return false;
    }

    [SubscribeLocalEvent]
    private void OnStepSterility(Entity<SurgeryStepComponent> ent, ref SurgeryStepEvent args)
    {
        if (AnyonePpe(args.User, args.Body))
            return;

        var damage = new DamageSpecifier();
        damage.DamageDict["Poison"] = UnsterileToxin * GetTableToxin(args.Body); // Inferus - operating table
        _damageable.TryChangeDamage(args.Body, damage, ignoreResistances: true, interruptsDoAfters: false);

        _popup.PopupEntity(Loc.GetString("surgery-unsterile-toxin"), args.Body, args.User, PopupType.SmallCaution);
    }
}
