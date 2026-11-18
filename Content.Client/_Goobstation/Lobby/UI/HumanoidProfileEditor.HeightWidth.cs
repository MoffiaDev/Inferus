// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 MarkerWicker <markerWicker@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Physics;

namespace Content.Client.Lobby.UI;

// Goobstation - port EE height/width sliders
// Inferus - moved from HumanoidProfileEditor.xaml.cs into a partial to keep upstream edits small
public sealed partial class HumanoidProfileEditor
{
    private enum SliderUpdate
    {
        Height,
        Width,
        Both,
    }

    private SpeciesPrototype? GetHeightWidthSpecies()
    {
        if (_species.Count == 0)
            return null;

        return _species.Find(x => x.ID == Profile?.Species) ?? _species.First();
    }

    private void InitializeHeightWidth()
    {
        HeightSlider.OnValueChanged += _ => UpdateDimensions(SliderUpdate.Height);
        WidthSlider.OnValueChanged += _ => UpdateDimensions(SliderUpdate.Width);

        // Inferus - look up the species when pressed instead of when the editor is built, so the reset follows species changes
        HeightReset.OnPressed += _ =>
        {
            if (GetHeightWidthSpecies() is not { } species)
                return;

            HeightSlider.Value = species.DefaultHeight;
            UpdateDimensions(SliderUpdate.Height);
        };

        WidthReset.OnPressed += _ =>
        {
            if (GetHeightWidthSpecies() is not { } species)
                return;

            WidthSlider.Value = species.DefaultWidth;
            UpdateDimensions(SliderUpdate.Width);
        };

        UpdateHeightWidthSliders();
    }

    private void SetProfileHeight(float height)
    {
        Profile = Profile?.WithHeight(height);
    }

    private void SetProfileWidth(float width)
    {
        Profile = Profile?.WithWidth(width);
    }

    private void UpdateHeightWidthSliders()
    {
        if (Profile is null || GetHeightWidthSpecies() is not { } species)
            return;

        HeightSlider.MinValue = species.MinHeight;
        HeightSlider.MaxValue = species.MaxHeight;
        HeightSlider.SetValueWithoutEvent(Math.Clamp(Profile.Height, species.MinHeight, species.MaxHeight));

        WidthSlider.MinValue = species.MinWidth;
        WidthSlider.MaxValue = species.MaxWidth;
        WidthSlider.SetValueWithoutEvent(Math.Clamp(Profile.Width, species.MinWidth, species.MaxWidth));

        UpdateDimensions(SliderUpdate.Both);
    }

    private void UpdateDimensions(SliderUpdate updateType)
    {
        if (Profile == null || GetHeightWidthSpecies() is not { } species)
            return;

        var heightValue = Math.Clamp(HeightSlider.Value, species.MinHeight, species.MaxHeight);
        var widthValue = Math.Clamp(WidthSlider.Value, species.MinWidth, species.MaxWidth);
        var sizeRatio = species.SizeRatio;
        var ratio = heightValue / widthValue;

        if (updateType == SliderUpdate.Height || updateType == SliderUpdate.Both)
            if (ratio < 1 / sizeRatio || ratio > sizeRatio)
                widthValue = heightValue / (ratio < 1 / sizeRatio ? (1 / sizeRatio) : sizeRatio);

        if (updateType == SliderUpdate.Width || updateType == SliderUpdate.Both)
            if (ratio < 1 / sizeRatio || ratio > sizeRatio)
                heightValue = widthValue * (ratio < 1 / sizeRatio ? (1 / sizeRatio) : sizeRatio);

        heightValue = Math.Clamp(heightValue, species.MinHeight, species.MaxHeight);
        widthValue = Math.Clamp(widthValue, species.MinWidth, species.MaxWidth);

        HeightSlider.SetValueWithoutEvent(heightValue); // Inferus - was HeightSlider.Value, avoids re-entering this method
        WidthSlider.SetValueWithoutEvent(widthValue); // Inferus - was WidthSlider.Value

        // Inferus - Start - only touch the profile and preview when something actually changed
        if (!MathHelper.CloseTo(Profile.Height, heightValue) || !MathHelper.CloseTo(Profile.Width, widthValue))
        {
            SetProfileHeight(heightValue);
            SetProfileWidth(widthValue);
            ReloadProfilePreview();
        }
        // Inferus - End

        var height = MathF.Round(species.AverageHeight * heightValue);
        HeightLabel.Text = Loc.GetString("humanoid-profile-editor-height-label", ("height", (int) height));

        var width = MathF.Round(species.AverageWidth * widthValue);
        WidthLabel.Text = Loc.GetString("humanoid-profile-editor-width-label", ("width", (int) width));

        UpdateWeight();
    }

    private void UpdateWeight()
    {
        if (Profile == null || GetHeightWidthSpecies() is not { } species)
            return;

        if (_prototypeManager.TryIndex(species.Prototype, out var proto)
            && proto.TryGetComponent<FixturesComponent>(out var fixture, _entManager.ComponentFactory)
            && fixture.Fixtures.TryGetValue("fix1", out var fix1))
        {
            var radius = fix1.Shape.Radius;
            var density = fix1.Density;
            var avg = (Profile.Width + Profile.Height) / 2;
            var weight = MathF.Round(MathF.PI * MathF.Pow(radius * avg, 2) * density);
            WeightLabel.Text = Loc.GetString("humanoid-profile-editor-weight-label", ("weight", (int) weight));
        }
        else // Whelp, the fixture doesn't exist, guesstimate it instead
            WeightLabel.Text = Loc.GetString("humanoid-profile-editor-weight-label", ("weight", 71));

        SpriteView.InvalidateMeasure();
    }
}
