using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client.Body;

// Inferus adaptation of Superlagg's per-sprite marking layers
public sealed partial class VisualBodySystem
{
    private void ApplyLayeredMarking(Entity<VisualOrganMarkingsComponent> organ,
        Entity<SpriteComponent?> target,
        MarkingPrototype prototype,
        Marking marking)
    {
        if (!Resolve(target, ref target.Comp) ||
            !_sprite.LayerMapTryGet(target, prototype.BodyPart, out var bodyIndex, true) ||
            !_sprite.TryGetLayer(target, bodyIndex, out var bodyLayer, true))
            return;

        var offsets = new Dictionary<HumanoidVisualLayers, int>();
        var displacements = new Dictionary<HumanoidVisualLayers, int>();
        organ.Comp.MarkingsDisplacement.TryGetValue(prototype.BodyPart, out var displacement);

        for (var i = 0; i < prototype.Sprites.Count; i++)
        {
            if (prototype.Sprites[i] is not SpriteSpecifier.Rsi sprite)
                continue;

            var slot = prototype.GetSpriteLayer(sprite);
            if (!_sprite.LayerMapTryGet(target, slot, out var anchor, true))
                continue;

            var offset = offsets.GetValueOrDefault(slot);
            var displaced = displacements.GetValueOrDefault(slot);
            var layerId = $"{prototype.ID}-{sprite.RsiState}";

            if (!_sprite.LayerMapTryGet(target, layerId, out var layer, false))
            {
                layer = _sprite.AddLayer(target, sprite, anchor + offset + displaced + 1);
                _sprite.LayerMapSet(target, layerId, layer);
                _sprite.LayerSetSprite(target, layer, sprite);
                _sprite.LayerSetVisible(target, layer, bodyLayer.Visible);
            }

            _sprite.LayerSetColor(target, layer, prototype.GetSpriteColor(marking.MarkingColors, i));

            // Only the original body slot uses that organ's displacement map.
            // Split tails and wings are already drawn for their separate planes.
            if (displacement != null && prototype.CanBeDisplaced && slot == prototype.BodyPart)
            {
                _displacement.TryAddDisplacement(displacement,
                    (target, target.Comp),
                    anchor + offset + 1,
                    layerId,
                    out _);
                displacements[slot] = displaced + 1;
            }

            if (prototype.Shaders != null &&
                prototype.Shaders.TryGetValue(sprite.RsiState, out var shader) &&
                _sprite.LayerMapTryGet(target, layerId, out var shaderLayer, false))
                target.Comp.LayerSetShader(shaderLayer, shader);

            offsets[slot] = offset + 1;
        }
    }
}
