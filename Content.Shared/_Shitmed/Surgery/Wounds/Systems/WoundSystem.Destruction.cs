// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Shared/_Shitmed/Surgery/Wounds/Systems/WoundSystem.Destruction.cs) // Inferus
// Inferus - a limb is amputated through DetachableOrganSystem, which moves the organ and every organ below it into a detached body.
using System.Linq;
using Content.Shared._Shitmed.Surgery.Operations; // Inferus
using Content.Shared._Shitmed.Surgery.Wounds.Components;
using Content.Shared.Body;
using Content.Shared.FixedPoint;

namespace Content.Shared._Shitmed.Surgery.Wounds.Systems;

public sealed partial class WoundSystem
{
    /// <summary>
    /// Chance that a hit on a limb with no integrity left tears it off, per damage type.
    /// </summary>
    private static readonly Dictionary<string, float> DismemberChances = new()
    {
        ["Slash"] = 0.25f,
        ["Blunt"] = 0.1f,
        ["Piercing"] = 0.05f,
        ["Heat"] = 0.1f,
    };

    private static readonly FixedPoint2 DismemberMinimumDamage = 5;

    [Dependency] private DetachableOrganSystem _detachable = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private EntityQuery<VisualOrganComponent> _visualOrganQuery = default!;
    [Dependency] private OrganRelationSystem _organRelation = default!;
    [Dependency] private EntityQuery<DetachableOrganComponent> _detachableQuery = default!;

    private bool TryDismember(
        Entity<WoundDamageModelComponent> body,
        List<Entity<OrganComponent, WoundableComponent>> woundables,
        Entity<OrganComponent, WoundableComponent> limb,
        string type,
        FixedPoint2 amount)
    {
        if (amount < DismemberMinimumDamage
            || !DismemberChances.TryGetValue(type, out var chance)
            || !woundables.Any(w => w.Owner == limb.Owner)
            || _random.NextFloat() >= chance)
            return false;

        return AmputateCore(body.Comp, woundables, limb);
    }

    /// <summary>
    /// Tears a limb, and everything attached below it, off its body.
    /// </summary>
    /// <param name="limbUid">The limb organ to amputate.</param>
    /// <param name="force">Cut it off even if the limb cannot be removed by damage, like the head. Inferus.</param>
    /// <returns>True if the limb was detached.</returns>
    public bool TryAmputate(EntityUid limbUid, bool force = false)
    {
        if (!TryComp<OrganComponent>(limbUid, out var organ)
            || !_woundableQuery.TryComp(limbUid, out var woundable)
            || organ.Body is not { } bodyUid)
            return false;

        TryComp<WoundDamageModelComponent>(bodyUid, out var model);

        if (!AmputateCore(model, null, (limbUid, organ, woundable), force)) // Inferus
            return false;

        if (model != null)
            SyncBodyDamage((bodyUid, model), _body.EnumerateOrgans<WoundableComponent>(bodyUid).ToList());

        return true;
    }

    private bool AmputateCore(
        WoundDamageModelComponent? model,
        List<Entity<OrganComponent, WoundableComponent>>? woundables,
        Entity<OrganComponent, WoundableComponent> limb,
        bool force = false) // Inferus
    {
        if (_net.IsClient
            || (!limb.Comp2.CanRemove && !force) // Inferus
            || !_detachableQuery.HasComp(limb.Owner)
            || limb.Comp1.Body == null)
            return false;

        var lost = new List<Entity<OrganComponent, WoundableComponent>> { limb };
        foreach (var child in _organRelation.AllChildren(limb.Owner))
        {
            if (TryComp<OrganComponent>(child, out var childOrgan) && _woundableQuery.TryComp(child, out var childWoundable))
                lost.Add((child.Owner, childOrgan, childWoundable));
        }

        // The detach below changes limb.Comp1.Body, so keep the body the limb is leaving.
        var bodyUid = limb.Comp1.Body.Value;
        var bodyName = Name(bodyUid);
        var limbName = Name(limb.Owner);
        var parentUid = TryComp<ChildOrganComponent>(limb.Owner, out var limbRelation) ? limbRelation.Parent : null; // Inferus - surgery reattaches the limb here

        // The detached body is what lies on the floor, so it takes the limb's name.
        var detached = _detachable.Detach(limb.Owner);
        if (detached == null)
            return false;

        // Inferus - remembers what the limb was attached to, so surgery can put it back
        var detachedLimb = EnsureComp<DetachedLimbComponent>(detached.Value);
        detachedLimb.Limb = limb.Owner;
        detachedLimb.Parent = parentUid;

        _metaData.SetEntityName(detached.Value, Loc.GetString("shitmed-detached-limb-name", ("owner", bodyName), ("limb", limbName)));
        _metaData.SetEntityDescription(detached.Value, Loc.GetString("shitmed-detached-limb-description"));

        foreach (var part in lost)
        {
            // The body keeps the damage the lost limb held, so losing a limb does not heal the body.
            if (model != null)
            {
                foreach (var wound in GetWoundableWounds(part.Owner, part.Comp2))
                {
                    if (wound.Comp.IsScar)
                        continue;

                    var type = (string) wound.Comp.DamageType;
                    model.BodyDamage.DamageDict[type] = model.BodyDamage.DamageDict.GetValueOrDefault(type) + wound.Comp.WoundSeverityPoint;
                }
            }

            part.Comp2.WoundableSeverity = WoundableSeverity.Severed;
            Dirty(part.Owner, part.Comp2);

            // Damage overlays are separate sprite layers, so hide the one that sat on the layer this part drew.
            if (_visualOrganQuery.TryComp(part.Owner, out var visual) && HasComp<AppearanceComponent>(bodyUid))
                _appearance.SetData(bodyUid, visual.Layer, true);
        }

        woundables?.RemoveAll(w => lost.Any(l => l.Owner == w.Owner));
        return true;
    }
}
