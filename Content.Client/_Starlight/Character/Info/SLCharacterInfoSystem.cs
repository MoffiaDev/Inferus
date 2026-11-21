// SPDX-FileCopyrightText: 2025 Starlight
// SPDX-License-Identifier: Starlight-MIT

using Content.Client._Starlight.Character.Info.UI;
using Content.Shared._Starlight.Character.Info;
using Content.Shared._Inferus.Character.Info; // Inferus

namespace Content.Client._Starlight.Character.Info;

public sealed partial class SLCharacterInfoSystem : SLSharedCharacterInfoSystem
{
    private readonly Dictionary<EntityUid, CharacterInspectWindow> _openInspectionWindows = new();

    // Inferus - minds are only networked to their own player, so ask the server for mind-held info
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CharacterInspectInfoEvent>(OnInspectInfo);
    }

    private void OnInspectInfo(CharacterInspectInfoEvent ev)
    {
        if (!TryGetEntity(ev.Target, out var target)
            || !_openInspectionWindows.TryGetValue(target.Value, out var window))
            return;

        window.SetMindInfo(ev.PersonalityDescription, ev.OOCNotes);
    }
    // End Inferus

    public override void Shutdown()
    {
        base.Shutdown();

        foreach (var window in _openInspectionWindows.Values)
        {
            window.Close();
        }

        _openInspectionWindows.Clear();
    }

    protected override void OpenCharacterWindow(EntityUid target, EntityUid requester)
    {
        if (!target.Valid)
            return;

        var viewer = requester.Valid ? requester : target;

        if (target != viewer) // Inferus: your own mind is already networked to you
            RaiseNetworkEvent(new RequestCharacterInspectInfoEvent(GetNetEntity(target))); // Inferus

        if (_openInspectionWindows.TryGetValue(target, out var window))
        {
            window.SetCharacter(target, EntityManager, viewer);
            window.OpenCentered();
            return;
        }

        window = new CharacterInspectWindow();
        window.SetCharacter(target, EntityManager, viewer);

        _openInspectionWindows[target] = window;

        window.OnClose += () => _openInspectionWindows.Remove(target);
        window.Title = Loc.GetString("character-info-window-title", ("player", target));
        window.OpenCentered();
    }
}
