using System.Linq;

namespace Content.Shared.Humanoid.Markings;

public sealed partial class MarkingPrototype
{
    /// <summary>
    /// Maps the new sprite states to their old color indices when a marking gains layers.
    /// Only applies to profiles with the exact legacy color count; new slots default to white.
    /// </summary>
    [DataField]
    public Dictionary<string, int>? LegacyColorIndices { get; private set; }

    public bool TryMapLegacyColors(IReadOnlyList<Color> colors, out List<Color> mapped)
    {
        mapped = new();
        if (LegacyColorIndices is not { Count: > 0 } indices ||
            colors.Count == Sprites.Count ||
            colors.Count != indices.Values.Max() + 1)
            return false;

        for (var i = 0; i < Sprites.Count; i++)
        {
            var name = GetSpriteName(Sprites[GetColorIndex(i)]);
            mapped.Add(name != null && indices.TryGetValue(name, out var index) &&
                       index >= 0 && index < colors.Count
                ? colors[index]
                : Color.White);
        }

        return true;
    }
}
