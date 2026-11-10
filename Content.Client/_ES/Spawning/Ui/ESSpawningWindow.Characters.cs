using Content.Client.Lobby.UI;
using Content.Shared.Preferences;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Player;

namespace Content.Client._ES.Spawning.Ui;

// Inferus: adapt Moffstation's explicit late-join character selection to the ES window
public sealed partial class ESSpawningWindow
{
    [Dependency] private ISharedPlayerManager _characterPlayerManager = default!;

    private int? _selectedCharacterSlot;

    private void InitializeCharacterPicker()
    {
        RebuildCharacterPicker();
        _preferencesManager.OnServerDataLoaded += RefreshCharacterPicker;
    }

    private void RefreshCharacterPicker()
    {
        RebuildCharacterPicker();
        RebuildJobLists();
    }

    private void RebuildCharacterPicker()
    {
        CharacterList.RemoveAllChildren();

        if (_preferencesManager.Preferences is not { } preferences)
        {
            _selectedCharacterSlot = null;
            return;
        }

        if (_selectedCharacterSlot is { } selected && !preferences.Characters.ContainsKey(selected))
            _selectedCharacterSlot = null;

        if (_selectedCharacterSlot == null && preferences.Characters.ContainsKey(preferences.SelectedCharacterIndex))
            _selectedCharacterSlot = preferences.SelectedCharacterIndex;

        var group = new ButtonGroup();
        foreach (var (slot, profile) in preferences.Characters)
        {
            _selectedCharacterSlot ??= slot;
            var button = new CharacterPickerButton(
                _prototypeManager,
                _characterPlayerManager,
                group,
                profile,
                slot == _selectedCharacterSlot,
                simple: true)
            {
                // Inferus: keep cards readable and let the strip scroll instead of compressing them.
                MinWidth = 280,
                HorizontalExpand = false,
                // End Inferus
            };

            button.OnPressed += _ =>
            {
                _selectedCharacterSlot = slot;
                RebuildJobLists();
            };

            CharacterList.AddChild(button);
        }
    }

    private HumanoidCharacterProfile? GetSelectedCharacterProfile()
    {
        if (_selectedCharacterSlot is not { } slot)
            return null;

        return _preferencesManager.Preferences?.Characters.GetValueOrDefault(slot);
    }
}
