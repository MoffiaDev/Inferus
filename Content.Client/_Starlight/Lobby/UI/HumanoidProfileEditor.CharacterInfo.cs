// SPDX-FileCopyrightText: 2025 Starlight
// SPDX-License-Identifier: Starlight-MIT

using Content.Client._CD.Records.UI;
using Content.Client._Starlight.Lobby.UI;
using Content.Shared._CD.Records;
using Content.Shared._Starlight.CCVar;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

// ReSharper disable once CheckNamespace
namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private ScrollContainer? _icInfoTab;
    private CharacterInfoEditor? _icInfoEditor;
    private ScrollContainer? _oocInfoTab;
    private OOCInfoEditor? _oocInfoEditor;
    private RecordEditorGui? _recordsTab; // Cosmatic Drift Record System

    /// <summary>
    /// Adds or removes the IC info, OOC info and records tabs, depending on the server settings.
    /// </summary>
    public void RefreshStarlightCharacterInfo()
    {
        var allowRpNotes = _cfgManager.GetCVar(StarlightCCVars.OOCNotes);

        if (_allowFlavorText && _icInfoEditor == null)
        {
            _icInfoEditor = new CharacterInfoEditor();
            _icInfoEditor.PhysicalDescInput.OnTextChanged += OnPhysicalDescChanged;
            _icInfoEditor.PersonalityDescInput.OnTextChanged += OnPersonalityDescChanged;
            _icInfoEditor.PhysicalDescInput.TextRope = new Rope.Leaf(Profile?.PhysicalDescription ?? "");
            _icInfoEditor.PersonalityDescInput.TextRope = new Rope.Leaf(Profile?.PersonalityDescription ?? "");
            _icInfoTab = AddCharacterInfoTab(_icInfoEditor, "humanoid-profile-editor-ic-info-tab");
        }
        else if (!_allowFlavorText && _icInfoTab != null)
        {
            TabContainer.RemoveChild(_icInfoTab);
            _icInfoTab.Dispose();
            _icInfoTab = null;
            _icInfoEditor = null;
        }

        if (allowRpNotes && _oocInfoEditor == null)
        {
            _oocInfoEditor = new OOCInfoEditor();
            _oocInfoEditor.PersonalNotesInput.OnTextChanged += OnPersonalNotesChanged;
            _oocInfoEditor.OOCNotesInput.OnTextChanged += OnOOCNotesChanged;
            _oocInfoEditor.PersonalNotesInput.TextRope = new Rope.Leaf(Profile?.PersonalNotes ?? "");
            _oocInfoEditor.OOCNotesInput.TextRope = new Rope.Leaf(Profile?.OOCNotes ?? "");
            _oocInfoTab = AddCharacterInfoTab(_oocInfoEditor, "humanoid-profile-editor-ooc-info-tab");
        }
        else if (!allowRpNotes && _oocInfoTab != null)
        {
            TabContainer.RemoveChild(_oocInfoTab);
            _oocInfoTab.Dispose();
            _oocInfoTab = null;
            _oocInfoEditor = null;
        }

        // Cosmatic Drift Record System-start: Build the CD record editor tab and hook persistence callbacks
        if (_recordsTab == null)
        {
            // Create the record editor in code so that we can provide a callback for saving edits back to the profile.
            _recordsTab = new RecordEditorGui(UpdateProfileRecords)
            {
                HorizontalExpand = true,
                VerticalExpand = true,
            };

            TabContainer.AddChild(_recordsTab);
            TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString("humanoid-profile-editor-cd-records-tab"));
            _recordsTab.Update(Profile);
        }
        // Cosmatic Drift Record System-end
    }

    private ScrollContainer AddCharacterInfoTab(Control editor, string titleLocId)
    {
        var tab = new ScrollContainer
        {
            HScrollEnabled = false,
            VerticalExpand = true,
            Margin = new Thickness(10),
        };
        tab.AddChild(editor);

        TabContainer.AddChild(tab);
        TabContainer.SetTabTitle(TabContainer.ChildCount - 1, Loc.GetString(titleLocId));
        return tab;
    }

    /// <summary>
    /// Fills the IC info, OOC info and records tabs from the current profile.
    /// </summary>
    private void UpdateStarlightCharacterInfoEdit()
    {
        if (_icInfoEditor != null)
        {
            _icInfoEditor.PhysicalDescInput.TextRope = new Rope.Leaf(Profile?.PhysicalDescription ?? "");
            _icInfoEditor.PersonalityDescInput.TextRope = new Rope.Leaf(Profile?.PersonalityDescription ?? "");
        }

        if (_oocInfoEditor != null)
        {
            _oocInfoEditor.PersonalNotesInput.TextRope = new Rope.Leaf(Profile?.PersonalNotes ?? "");
            _oocInfoEditor.OOCNotesInput.TextRope = new Rope.Leaf(Profile?.OOCNotes ?? "");
        }

        _recordsTab?.Update(Profile); // Cosmatic Drift Record System: Refresh record editor when a profile is loaded
        UpdateCustomSpecieNameEdit();
    }

    /// <summary>
    /// Keeps the record height and weight in line with the height/width sliders.
    /// </summary>
    private void UpdateCDRecordMetrics()
    {
        _recordsTab?.UpdateComputedMetrics(Profile);
    }

    // Cosmatic Drift Record System: Persist record edits from the CD tab back onto the profile
    private void UpdateProfileRecords(PlayerProvidedCharacterRecords records)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithCDCharacterRecords(records);
        SetDirty();
    }

    private void OnPhysicalDescChanged(TextEdit.TextEditEventArgs args)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithPhysicalDesc(Rope.Collapse(args.TextRope).Trim());
        SetDirty();
    }

    private void OnPersonalityDescChanged(TextEdit.TextEditEventArgs args)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithPersonalityDesc(Rope.Collapse(args.TextRope).Trim());
        SetDirty();
    }

    private void OnPersonalNotesChanged(TextEdit.TextEditEventArgs args)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithPersonalNotes(Rope.Collapse(args.TextRope).Trim());
        SetDirty();
    }

    private void OnOOCNotesChanged(TextEdit.TextEditEventArgs args)
    {
        if (Profile is null)
            return;

        Profile = Profile.WithOOCNotes(Rope.Collapse(args.TextRope).Trim());
        SetDirty();
    }
}
