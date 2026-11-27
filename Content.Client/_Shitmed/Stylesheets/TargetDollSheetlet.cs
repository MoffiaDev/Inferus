// SPDX-License-Identifier: AGPL-3.0-or-later
// Adapted from Goob-Station (Content.Client/Stylesheets/StyleNano.cs, Shitmed section) // Inferus
using Content.Client.Stylesheets.Stylesheets;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;
using static Content.Client.Stylesheets.StylesheetHelpers;

namespace Content.Client.Stylesheets.Sheetlets;

[CommonSheetlet]
public sealed class TargetDollSheetlet<T> : Sheetlet<T> where T : PalettedStylesheet
{
    private const string DollRoot = "/Textures/_Shitmed/Interface/Targeting/Doll/";

    public override StyleRule[] GetRules(T sheet, object config)
    {
        var cache = IoCManager.Resolve<IResourceCache>();
        Texture Tex(string name) => cache.GetResource<TextureResource>(new ResPath(DollRoot + name + ".png")).Texture;

        return
        [
            E<TextureButton>()
                .Class("TargetDollButtonHead")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, Tex("head_hover")),
            E<TextureButton>()
                .Class("TargetDollButtonChest")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, Tex("torso_hover")),
            E<TextureButton>()
                .Class("TargetDollButtonGroin")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, Tex("groin_hover")),
            E<TextureButton>()
                .Class("TargetDollButtonLeftArm")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, Tex("leftarm_hover")),
            E<TextureButton>()
                .Class("TargetDollButtonLeftHand")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, Tex("lefthand_hover")),
            E<TextureButton>()
                .Class("TargetDollButtonRightArm")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, Tex("rightarm_hover")),
            E<TextureButton>()
                .Class("TargetDollButtonRightHand")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, Tex("righthand_hover")),
            E<TextureButton>()
                .Class("TargetDollButtonLeftLeg")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, Tex("leftleg_hover")),
            E<TextureButton>()
                .Class("TargetDollButtonLeftFoot")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, Tex("leftfoot_hover")),
            E<TextureButton>()
                .Class("TargetDollButtonRightLeg")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, Tex("rightleg_hover")),
            E<TextureButton>()
                .Class("TargetDollButtonRightFoot")
                .Pseudo(TextureButton.StylePseudoClassHover)
                .Prop(TextureButton.StylePropertyTexture, Tex("rightfoot_hover")),
        ];
    }
}
