// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - opens the surgery window when the server sends the state, and sends back the step that was pressed.
using Content.Shared._Shitmed.Surgery.Operations;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Shitmed.Surgery;

public sealed class SurgerySystem : EntitySystem
{
    private const float CloseRange = 3f;

    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private SurgeryWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SurgeryStateEvent>(OnState);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _window?.Close();
        _window = null;
    }

    private void OnState(SurgeryStateEvent ev)
    {
        if (_window == null)
        {
            _window = new SurgeryWindow();
            _window.OnStepPressed += OnStepPressed;
            _window.OnClose += () => _window = null;
            _window.SetState(ev);
            _window.OpenCentered();
            return;
        }

        _window.SetState(ev);
    }

    private void OnStepPressed(NetEntity body, NetEntity part, EntProtoId surgery, EntProtoId step)
    {
        RaiseNetworkEvent(new SurgeryStepChosenEvent(body, part, surgery, step));
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_window?.Body is not { } netBody)
            return;

        // Walking away or losing the patient closes the window.
        if (!TryGetEntity(netBody, out var body)
            || _player.LocalEntity is not { } user
            || !_transform.InRange(Transform(user).Coordinates, Transform(body.Value).Coordinates, CloseRange))
        {
            _window.Close();
        }
    }
}
