using Content.Shared._Shitmed.Surgery.Organs; // Inferus
using Content.Shared.Emp; // Inferus
using Content.Shared.Hands.EntitySystems;

namespace Content.Shared.Body;

public sealed partial class HandOrganSystem : EntitySystem
{
    [Dependency] private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HandOrganComponent, OrganGotInsertedEvent>(OnGotInserted);
        SubscribeLocalEvent<HandOrganComponent, OrganGotRemovedEvent>(OnGotRemoved);
    }

    private void OnGotInserted(Entity<HandOrganComponent> ent, ref OrganGotInsertedEvent args)
    {
        // Inferus - a hand that an EMP has switched off is not given to the body, it comes when the EMP is over
        if (HasComp<EmpDisabledComponent>(ent))
            return;

        _hands.AddHand(args.Target, ent.Comp.HandID, ent.Comp.Data);
    }

    private void OnGotRemoved(Entity<HandOrganComponent> ent, ref OrganGotRemovedEvent args)
    {
        // prevent a recursive double-delete bug
        if (LifeStage(args.Target) >= EntityLifeStage.Terminating)
            return;

        // Inferus - the body does not have the hand of a switched off organ
        if (HasComp<EmpDisabledHandComponent>(ent))
            return;

        _hands.RemoveHand(args.Target, ent.Comp.HandID);
    }

    /// <summary>
    /// Inferus - gives the hand to the body or takes it away again without taking the organ out. An EMP does this to a prosthetic hand.
    /// </summary>
    public void SetEnabled(Entity<HandOrganComponent> ent, EntityUid body, bool enabled)
    {
        if (enabled)
            _hands.AddHand(body, ent.Comp.HandID, ent.Comp.Data);
        else
            _hands.RemoveHand(body, ent.Comp.HandID);
    }
}
