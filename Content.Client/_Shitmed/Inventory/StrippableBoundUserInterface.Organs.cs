// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Starlight (Content.Client/_Starlight/Inventory/StrippableBoundUserInterface.Starlight.cs) // Inferus
// Inferus - the storage organs of the body, like the cargo legs, get a button in the strip menu below the slots.
using Content.Client.UserInterface.Controls;
using Content.Shared._Shitmed.Surgery.Organs;
using Content.Shared.Body;
using Content.Shared.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using static Robust.Client.UserInterface.Control;

namespace Content.Client.Inventory;

public sealed partial class StrippableBoundUserInterface
{
    private void AddOrganStorageButtons()
    {
        var bodySystem = EntMan.System<BodySystem>();
        var columnCount = Math.Max(4, _inventoryDimensions.X + 1);
        var firstRow = _inventoryDimensions.Y + 1;
        var index = 0;

        foreach (var organ in bodySystem.EnumerateOrgans<OrganStorageComponent>(Owner))
        {
            var organUid = organ.Owner;
            var button = new SlotButton
            {
                ButtonTexturePath = "Slots/hand_m",
                Name = $"OrganStorage_{organUid}",
            };
            button.Pressed += (ev, slot) => OrganStorageSlotPressed(ev, slot, organUid);
            UpdateEntityIcon(button, organUid);
            _strippingMenu!.InventoryContainer.AddChild(button);

            var position = new Vector2i(index % columnCount, firstRow + (index / columnCount));
            index++;

            LayoutContainer.SetPosition(button, position * (SlotControl.DefaultButtonSize + ButtonSeparation));
            if (position.X > _inventoryDimensions.X)
                _inventoryDimensions = new Vector2i(position.X, _inventoryDimensions.Y);
            if (position.Y > _inventoryDimensions.Y)
                _inventoryDimensions = new Vector2i(_inventoryDimensions.X, position.Y);
        }
    }

    private void OrganStorageSlotPressed(GUIBoundKeyEventArgs ev, SlotControl slot, EntityUid organ)
    {
        if (ev.Function == EngineKeyFunctions.Use)
        {
            SendPredictedMessage(new StrippingOrganStorageButtonPressed(EntMan.GetNetEntity(organ)));
            ev.Handle();
        }
        else if (ev.Function == ContentKeyFunctions.ExamineEntity)
        {
            _examine.DoExamine(organ);
            ev.Handle();
        }
    }
}
