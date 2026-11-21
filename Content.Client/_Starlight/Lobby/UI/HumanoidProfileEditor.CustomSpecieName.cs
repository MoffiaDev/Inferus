// SPDX-FileCopyrightText: 2025 Starlight
// SPDX-License-Identifier: Starlight-MIT

using System.Linq;

// ReSharper disable once CheckNamespace
namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void InitializeCustomSpecieName()
    {
        CCustomSpecieNameEdit.OnTextChanged += args => SetCustomSpecieName(args.Text);
    }

    private void SetCustomSpecieName(string customName)
    {
        if (Profile == null || Profile.CustomSpecieName == customName)
            return;

        Profile = Profile.WithCustomSpecieName(customName);
        SetDirty();
    }

    private void UpdateCustomSpecieNameEdit()
    {
        if (_species.Count == 0)
            return;

        var species = _species.Find(x => x.ID == Profile?.Species) ?? _species.First();
        CCustomSpecieNameEdit.PlaceHolder = Loc.GetString(species.Name);

        var text = Profile?.CustomSpecieName ?? string.Empty;
        if (CCustomSpecieNameEdit.Text != text)
            CCustomSpecieNameEdit.Text = text;
    }
}
