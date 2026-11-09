using Robust.Shared.Utility;

namespace Content.Shared.Humanoid.Markings;

// Inferus adaptation of Superlagg's marking layers and linked colors.
public sealed partial class MarkingPrototype
{
    /// <summary>
    /// Overrides the visual layer of individual sprite states without changing the marking's selection slot.
    /// </summary>
    [DataField]
    public Dictionary<string, HumanoidVisualLayers>? Layering { get; private set; }

    /// <summary>
    /// Maps a child sprite state to the state whose color it uses.
    /// Linked children do not need their own color slider.
    /// </summary>
    [DataField]
    public Dictionary<string, string>? ColorLinks { get; private set; }

    public HumanoidVisualLayers GetSpriteLayer(SpriteSpecifier sprite)
    {
        var name = GetSpriteName(sprite);
        return name != null && Layering != null && Layering.TryGetValue(name, out var layer)
            ? layer
            : BodyPart;
    }

    public static string? GetSpriteName(SpriteSpecifier sprite)
    {
        return sprite switch
        {
            SpriteSpecifier.Rsi rsi => rsi.RsiState,
            SpriteSpecifier.Texture texture => texture.TexturePath.Filename,
            _ => null,
        };
    }

    /// <summary>
    /// Resolves linked colors independently of dictionary order. Invalid or cyclic links retain their own color.
    /// </summary>
    public int GetColorIndex(int spriteIndex)
    {
        if (ColorLinks == null || spriteIndex < 0 || spriteIndex >= Sprites.Count)
            return spriteIndex;

        var current = spriteIndex;
        for (var depth = 0; depth < Sprites.Count; depth++)
        {
            var name = GetSpriteName(Sprites[current]);
            if (name == null || !ColorLinks.TryGetValue(name, out var parent))
                return current;

            var parentIndex = Sprites.FindIndex(sprite => GetSpriteName(sprite) == parent);
            if (parentIndex < 0)
                return spriteIndex;

            current = parentIndex;
        }

        return spriteIndex;
    }

    public Color GetSpriteColor(IReadOnlyList<Color>? colors, int spriteIndex)
    {
        var index = GetColorIndex(spriteIndex);
        return colors != null && index >= 0 && index < colors.Count ? colors[index] : Color.White;
    }
}
