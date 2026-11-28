// SPDX-License-Identifier: AGPL-3.0-or-later
// Inferus - the surgery window. Three columns: the limbs, the surgeries the selected limb can have, and the steps of the selected surgery.
// Goob-Station builds this from xaml and a bound user interface, here it is plain code and network events.
using System.Numerics;
using Content.Shared._Shitmed.Surgery.Operations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;

namespace Content.Client._Shitmed.Surgery;

public sealed class SurgeryWindow : DefaultWindow
{
    private readonly BoxContainer _parts = Column();
    private readonly BoxContainer _surgeries = Column();
    private readonly BoxContainer _steps = Column();

    private SurgeryStateEvent? _state;
    private NetEntity? _selectedPart;
    private EntProtoId? _selectedSurgery;

    public event Action<NetEntity, NetEntity, EntProtoId, EntProtoId>? OnStepPressed;

    public NetEntity? Body => _state?.Body;

    public SurgeryWindow()
    {
        Title = Loc.GetString("surgery-ui-window-title");
        SetSize = new Vector2(640, 380);

        var columns = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 6,
            VerticalExpand = true,
            HorizontalExpand = true,
        };

        columns.AddChild(Scroll(_parts, Loc.GetString("surgery-ui-window-parts")));
        columns.AddChild(Scroll(_surgeries, Loc.GetString("surgery-ui-window-surgeries")));
        columns.AddChild(Scroll(_steps, Loc.GetString("surgery-ui-window-steps")));

        Contents.AddChild(columns);
    }

    public void SetState(SurgeryStateEvent state)
    {
        _state = state;

        if (_selectedPart != null && !state.Parts.Exists(p => p.Part == _selectedPart))
        {
            _selectedPart = null;
            _selectedSurgery = null;
        }

        Rebuild();
    }

    private void Rebuild()
    {
        _parts.RemoveAllChildren();
        _surgeries.RemoveAllChildren();
        _steps.RemoveAllChildren();

        if (_state == null)
            return;

        var parts = new List<SurgeryPartInfo>(_state.Parts);
        parts.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        SurgeryPartInfo? selected = null;
        foreach (var part in parts)
        {
            var info = part;
            var button = new Button { Text = info.Name, Disabled = info.Part == _selectedPart };
            button.OnPressed += _ =>
            {
                _selectedPart = info.Part;
                _selectedSurgery = null;
                Rebuild();
            };
            _parts.AddChild(button);

            if (info.Part == _selectedPart)
                selected = info;
        }

        if (selected == null)
            return;

        var surgeries = new List<SurgeryInfo>(selected.Surgeries);
        surgeries.Sort((a, b) => a.Priority != b.Priority ? a.Priority.CompareTo(b.Priority) : string.Compare(a.Name, b.Name, StringComparison.Ordinal));

        SurgeryInfo? selectedSurgery = null;
        foreach (var surgery in surgeries)
        {
            var info = surgery;
            var button = new Button { Text = info.Name, Disabled = info.Id == _selectedSurgery };
            button.OnPressed += _ =>
            {
                _selectedSurgery = info.Id;
                Rebuild();
            };
            _surgeries.AddChild(button);

            if (info.Id == _selectedSurgery)
                selectedSurgery = info;
        }

        if (selectedSurgery == null)
            return;

        var next = selectedSurgery.Steps.FindIndex(s => !s.Complete);
        for (var i = 0; i < selectedSurgery.Steps.Count; i++)
        {
            var step = selectedSurgery.Steps[i];
            var partNet = selected.Part;
            var surgeryId = selectedSurgery.Id;

            var button = new Button
            {
                Text = step.Complete ? $"✓ {step.Name}" : step.Name,
                Disabled = i != next,
                ToolTip = step.Tool != null ? Loc.GetString("surgery-ui-window-steps-tool", ("tool", step.Tool)) : null,
            };

            if (step.Complete)
                button.Modulate = Color.LightGreen;

            button.OnPressed += _ =>
            {
                if (_state != null)
                    OnStepPressed?.Invoke(_state.Body, partNet, surgeryId, step.Id);
            };

            _steps.AddChild(button);
        }
    }

    private static BoxContainer Column()
    {
        return new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 2,
            HorizontalExpand = true,
        };
    }

    private static Control Scroll(BoxContainer content, string title)
    {
        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
            SizeFlagsStretchRatio = 1,
        };

        box.AddChild(new Label { Text = title, HorizontalAlignment = Control.HAlignment.Center });
        box.AddChild(new ScrollContainer
        {
            HScrollEnabled = false,
            VerticalExpand = true,
            HorizontalExpand = true,
            Children = { content },
        });

        return box;
    }
}
