using Content.Client.CharacterInfo;
using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared.Input;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using Content.Shared.Roles;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input.Binding;
using Robust.Shared.Prototypes;
using static Content.Client.CharacterInfo.CharacterInfoSystem;

namespace Content.Client._ES.Mind.Ui;

/// <summary>
/// Handles the <see cref="ESCharacterWindow"/>.
/// Replaces <see cref="Content.Client.UserInterface.Systems.Character.CharacterUIController"/>.
/// </summary>
[UsedImplicitly]
public sealed partial class ESCharacterUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>, IOnSystemChanged<CharacterInfoSystem>
{
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    [UISystemDependency] private readonly CharacterInfoSystem _characterInfo = default!;

    private ESCharacterWindow? _window;

    private MenuButton? CharacterButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.CharacterButton;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<MindRoleTypeChangedEvent>(OnRoleTypeChanged);
    }

    public void OnStateEntered(GameplayState state)
    {
        _window = UIManager.CreateWindow<ESCharacterWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);

        _window.OnClose += DeactivateButton;
        _window.OnOpen += ActivateButton;

        _player.LocalPlayerAttached += OnLocalPlayerAttached;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenCharacterMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<ESCharacterUIController>();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.Close();
            _window = null;
        }

        _player.LocalPlayerAttached -= OnLocalPlayerAttached;

        CommandBinds.Unregister<ESCharacterUIController>();
    }

    public void OnSystemLoaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate += CharacterUpdated;
        _player.LocalPlayerDetached += OnLocalPlayerDetached;
    }

    public void OnSystemUnloaded(CharacterInfoSystem system)
    {
        system.OnCharacterUpdate -= CharacterUpdated;
        _player.LocalPlayerDetached -= OnLocalPlayerDetached;
    }

    private void CharacterUpdated(CharacterData data)
    {
        if (_window == null)
            return;

        _window.Update(data, _characterInfo.GetCharacterInfoControls(data.Entity));
        UpdateRoleType();
    }

    private void OnRoleTypeChanged(MindRoleTypeChangedEvent ev, EntitySessionEventArgs _)
    {
        UpdateRoleType();
    }

    private void UpdateRoleType()
    {
        if (_window == null)
            return;

        if (!_ent.TryGetComponent<MindContainerComponent>(_player.LocalEntity, out var container)
            || container.Mind is null)
            return;

        if (!_ent.TryGetComponent<MindComponent>(container.Mind.Value, out var mind))
            return;

        if (!_prototype.TryIndex(mind.RoleType, out var proto))
            Log.Error($"Player '{_player.LocalSession}' has invalid Role Type '{mind.RoleType}'. Displaying default instead");

        _window.SetRoleType(proto);
    }

    private void DeactivateButton()
    {
        if (CharacterButton != null)
            CharacterButton.Pressed = false;
    }

    private void ActivateButton()
    {
        if (CharacterButton != null)
            CharacterButton.Pressed = true;
    }

    private void CharacterButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        ToggleWindow();
    }

    private void OnLocalPlayerAttached(EntityUid obj)
    {
        if (_window is { IsOpen: true })
            _characterInfo.RequestCharacterInfo();
    }

    private void OnLocalPlayerDetached(EntityUid obj)
    {
        _window?.Close();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;

        CharacterButton?.SetClickPressed(!_window.IsOpen);

        if (_window.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _characterInfo.RequestCharacterInfo();
            _window.Open();
        }
    }

    public void UnloadButton()
    {
        if (CharacterButton == null)
            return;

        CharacterButton.OnPressed -= CharacterButtonPressed;
    }

    public void LoadButton()
    {
        if (CharacterButton == null)
            return;

        CharacterButton.OnPressed += CharacterButtonPressed;
    }
}
