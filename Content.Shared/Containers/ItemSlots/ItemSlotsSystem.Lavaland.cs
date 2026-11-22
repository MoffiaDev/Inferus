// Lavaland Change: delayed insertion/ejection for item slots.
// Ported from Goob Station (https://github.com/Goob-Station/Goob-Station/pull/5270), AGPL-3.0-or-later.
// Inferus: rewritten against the refactored upstream ItemSlotsSystem.
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Containers.ItemSlots;

public sealed partial class ItemSlotsSystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    [SubscribeLocalEvent]
    private void OnItemSlotDoAfter(Entity<ItemSlotsComponent> ent, ref ItemSlotInteractionDoAfterEvent args)
    {
        if (args.Handled
            || args.Cancelled
            || !ent.Comp.Slots.TryGetValue(args.SlotId, out var slot))
            return;

        args.Handled = true;

        if (args.TryEject && slot.HasItem)
        {
            if (CanEject(ent, slot, args.User, popup: args.User))
                TryEjectToHands(ent, slot, args.User, true, doAfter: false);
            return;
        }

        if (!args.TryInsert || args.Used is not { } used || !CanInsert(ent, slot, used, args.User, slot.Swap))
            return;

        if (_handsSystem.IsHolding(args.User, used) && !_handsSystem.TryDrop(args.User, used))
            return;

        if (slot.Item != null)
            _handsSystem.TryPickupAnyHand(args.User, slot.Item.Value);

        if (!Insert(ent, slot, used, args.User, excludeUserAudio: true))
            return;

        if (slot.InsertSuccessPopup.HasValue)
            _popupSystem.PopupClient(Loc.GetString(slot.InsertSuccessPopup), ent, args.User);
    }

    private bool TryStartInsertDoAfter(EntityUid uid, ItemSlot slot, EntityUid item, EntityUid user)
    {
        if (slot.InsertDelay is not { } delay || slot.ID == null)
            return false;

        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            delay,
            new ItemSlotInteractionDoAfterEvent(slot.ID, tryEject: false, tryInsert: true),
            uid,
            uid,
            item)
        {
            BreakOnHandChange = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
            BreakOnDamage = true,
        });
    }

    private bool TryStartEjectDoAfter(EntityUid uid, ItemSlot slot, EntityUid user)
    {
        if (slot.EjectDelay is not { } delay || slot.ID == null)
            return false;

        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            user,
            delay,
            new ItemSlotInteractionDoAfterEvent(slot.ID, tryEject: true, tryInsert: false),
            uid,
            uid)
        {
            BreakOnHandChange = true,
            BreakOnMove = true,
            BreakOnDropItem = true,
            BreakOnDamage = true,
        });
    }
}

/// <summary>
/// Lavaland Change: a do-after event for inserting into or ejecting from slots with a delay.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class ItemSlotInteractionDoAfterEvent : DoAfterEvent
{
    /// <summary>
    ///     The name of the slot/container from which to insert or eject an item.
    /// </summary>
    public string SlotId;

    /// <summary>
    ///     Whether to attempt to insert an item into the slot, if there is not already one inside.
    /// </summary>
    public bool TryInsert;

    /// <summary>
    ///     Whether to attempt to eject the item from the slot, if it has one.
    /// </summary>
    public bool TryEject;

    public ItemSlotInteractionDoAfterEvent(string slotId, bool tryEject = true, bool tryInsert = true)
    {
        SlotId = slotId;
        TryEject = tryEject;
        TryInsert = tryInsert;
    }

    public override DoAfterEvent Clone() => this;
}
