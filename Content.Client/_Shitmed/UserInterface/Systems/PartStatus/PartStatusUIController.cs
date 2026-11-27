// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Goob-Station (Content.Client/_Shitmed/UserInterface/Systems/PartStatus/PartStatusUIController.cs) // Inferus
using Content.Client._Shitmed.Targeting;
using Content.Client._Shitmed.UserInterface.Systems.PartStatus.Widgets;
using Content.Client.Gameplay;
using Content.Shared._Shitmed.Targeting;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Utility;

namespace Content.Client._Shitmed.UserInterface.Systems.PartStatus;

public sealed class PartStatusUIController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<TargetingSystem>
{
    [Dependency] private IEntityManager _entManager = default!;

    private SpriteSystem? _spriteSystem;
    private TargetingComponent? _targetingComponent;

    private PartStatusControl? PartStatusControl => UIManager.GetActiveUIWidgetOrNull<PartStatusControl>();

    public void OnSystemLoaded(TargetingSystem system)
    {
        system.PartStatusStartup += AddPartStatusControl;
        system.PartStatusShutdown += RemovePartStatusControl;
        system.PartStatusUpdate += UpdatePartStatusControl;
    }

    public void OnSystemUnloaded(TargetingSystem system)
    {
        system.PartStatusStartup -= AddPartStatusControl;
        system.PartStatusShutdown -= RemovePartStatusControl;
        system.PartStatusUpdate -= UpdatePartStatusControl;
    }

    public void OnStateEntered(GameplayState state)
    {
        if (PartStatusControl is not { } control)
            return;

        control.SetVisible(_targetingComponent != null);

        if (_targetingComponent != null)
            control.SetTextures(_targetingComponent.BodyStatus);
    }

    public void AddPartStatusControl(TargetingComponent component)
    {
        _targetingComponent = component;

        if (PartStatusControl is not { } control)
            return;

        control.SetVisible(true);
        control.SetTextures(component.BodyStatus);
    }

    public void RemovePartStatusControl()
    {
        PartStatusControl?.SetVisible(false);
        _targetingComponent = null;
    }

    public void UpdatePartStatusControl(TargetingComponent component)
    {
        if (PartStatusControl is { } control && _targetingComponent != null)
            control.SetTextures(_targetingComponent.BodyStatus);
    }

    public Texture GetTexture(SpriteSpecifier specifier)
    {
        _spriteSystem ??= _entManager.System<SpriteSystem>();
        return _spriteSystem.Frame0(specifier);
    }
}
