// SPDX-License-Identifier: MIT

using Content.Shared._Omu.Crewlet;
using Content.Shared.Sprite;
using Robust.Client.GameObjects;

namespace Content.Client._Omu.Crewlet;

public sealed partial class CrewletScaleSystem : EntitySystem // Inferus
{
    [Dependency] private SpriteSystem _sprite = default!; // Inferus

    public override void Initialize()
    {
        base.Initialize();

        UpdatesOutsidePrediction = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        using var query = EntityQueryEnumerator<CrewletComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var crewlet, out var sprite))
        {
            var scale = TryComp<ScaleVisualsComponent>(uid, out var visuals) ? visuals.Scale : crewlet.Scale;
            if (sprite.Scale != scale)
                _sprite.SetScale((uid, sprite), scale);
        }
    }
}
