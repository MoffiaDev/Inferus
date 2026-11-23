using Robust.Shared.Audio;

namespace Content.Shared._Inferus.Inventory;

/// <summary>
/// Sets the sounds an item plays when it is put into or taken out of an inventory slot.
/// When present, this overrides any clothing <c>equipSound</c> or <c>unequipSound</c>.
/// Items without this component or a clothing sound play <see cref="DefaultEquipSound"/> and <see cref="DefaultUnequipSound"/>.
/// </summary>
[RegisterComponent]
public sealed partial class ItemEquipSoundsComponent : Component
{
    public static readonly SoundSpecifier DefaultEquipSound =
        new SoundCollectionSpecifier("GenericEquip", AudioParams.Default.WithVolume(-6f));

    public static readonly SoundSpecifier DefaultUnequipSound =
        new SoundCollectionSpecifier("GenericEquip", AudioParams.Default.WithVolume(-8f));

    /// <summary>
    /// Played when the item is equipped into a slot. Null means silent.
    /// </summary>
    [DataField]
    public SoundSpecifier? EquipSound = DefaultEquipSound;

    /// <summary>
    /// Played when the item is removed from a slot. Null means silent.
    /// </summary>
    [DataField]
    public SoundSpecifier? UnequipSound = DefaultUnequipSound;
}
