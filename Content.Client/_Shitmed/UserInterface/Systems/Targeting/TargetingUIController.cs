// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from Goob-Station (Content.Client/_Shitmed/UserInterface/Systems/Targeting/TargetingUIController.cs) // Inferus
using Content.Client.Gameplay;
using Content.Client._Shitmed.UserInterface.Systems.Targeting.Widgets;
using Content.Shared._Shitmed.Targeting;
using Content.Client._Shitmed.Targeting;
using Content.Shared._Shitmed.Targeting.Events;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.Player;

namespace Content.Client._Shitmed.UserInterface.Systems.Targeting;

public sealed partial class TargetingUIController : UIController, IOnStateEntered<GameplayState>, IOnSystemChanged<TargetingSystem>
{
    [Dependency] private IEntityManager _entManager = default!; // Inferus - not readonly, class is partial
    [Dependency] private IEntityNetworkManager _net = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private TargetingComponent? _targetingComponent;
    private TargetingControl? TargetingControl => UIManager.GetActiveUIWidgetOrNull<TargetingControl>();

    public void OnSystemLoaded(TargetingSystem system)
    {
        system.TargetingStartup += AddTargetingControl;
        system.TargetingShutdown += RemoveTargetingControl;
        system.TargetChange += CycleTarget;
    }

    public void OnSystemUnloaded(TargetingSystem system)
    {
        system.TargetingStartup -= AddTargetingControl;
        system.TargetingShutdown -= RemoveTargetingControl;
        system.TargetChange -= CycleTarget;
    }

    public void OnStateEntered(GameplayState state)
    {
        if (TargetingControl == null)
            return;

        TargetingControl.SetTargetDollVisible(_targetingComponent != null);

        if (_targetingComponent != null)
            TargetingControl.SetBodyPartsVisible(_targetingComponent.Target);
    }

    public void AddTargetingControl(TargetingComponent component)
    {
        _targetingComponent = component;

        if (TargetingControl == null)
            return;

        TargetingControl.SetTargetDollVisible(true);
        TargetingControl.SetBodyPartsVisible(component.Target);
    }

    public void RemoveTargetingControl()
    {
        TargetingControl?.SetTargetDollVisible(false);
        _targetingComponent = null;
    }

    public void CycleTarget(TargetBodyPart bodyPart)
    {
        // Inferus - TryGetComponent instead of GetComponent so a missing component can't throw.
        if (_playerManager.LocalEntity is not { } user
            || !_entManager.TryGetComponent<TargetingComponent>(user, out var targetingComponent)
            || TargetingControl == null)
            return;

        if (bodyPart == targetingComponent.Target)
            return;

        _net.SendSystemNetworkMessage(new TargetChangeEvent(_entManager.GetNetEntity(user), bodyPart));
        TargetingControl.SetBodyPartsVisible(bodyPart);
    }
}
