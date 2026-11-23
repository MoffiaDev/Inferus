using Content.Shared._Inferus.Inventory;
using Content.Shared.Item;
using Robust.Shared.Audio;

namespace Content.Shared.Inventory;

// Inferus - every item plays an equip / unequip sound, even when it is not clothing
// or its clothing has no sound set. Pairs with the Goob Station item handling sounds port.
public abstract partial class InventorySystem
{
    /// <summary>
    /// Plays the fallback equip or unequip sound for an item that has no clothing sound of its own.
    /// </summary>
    private void PlayFallbackItemSound(EntityUid item, EntityUid target, EntityUid actor, bool unequip)
    {
        if (!HasComp<ItemComponent>(item))
            return;

        SoundSpecifier? sound = unequip
            ? ItemEquipSoundsComponent.DefaultUnequipSound
            : ItemEquipSoundsComponent.DefaultEquipSound;

        if (TryComp<ItemEquipSoundsComponent>(item, out var sounds))
            sound = unequip ? sounds.UnequipSound : sounds.EquipSound;

        _audio.PlayPredicted(sound, target, actor);
    }
}
