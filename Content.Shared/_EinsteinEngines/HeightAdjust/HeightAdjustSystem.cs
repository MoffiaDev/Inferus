// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 MarkerWicker <markerWicker@proton.me>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared.CCVar;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Sprite; // Inferus
using Robust.Shared.Configuration;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes; // Inferus
using Robust.Shared.Physics.Systems;

namespace Content.Shared._EinsteinEngines.HeightAdjust;

public sealed class HeightAdjustSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedContentEyeSystem _eye = default!;
    [Dependency] private readonly SharedScaleVisualsSystem _scaleVisuals = default!; // Inferus - was SharedHumanoidAppearanceSystem
    [Dependency] private readonly IConfigurationManager _config = default!;

    /// <summary>
    ///     Changes the density of fixtures and zoom of eyes based on a provided float scale
    /// </summary>
    /// <param name="uid">The entity to modify values for</param>
    /// <param name="scale">The scale to multiply values by</param>
    /// <returns>True if all operations succeeded</returns>
    public bool SetScale(EntityUid uid, float scale)
    {
        return SetScale(uid, new Vector2(scale, scale));
    }

    /// <summary>
    ///     Changes the density of fixtures and zoom of eyes based on a provided Vector2 scale
    /// </summary>
    /// <param name="uid">The entity to modify values for</param>
    /// <param name="scale">The scale to multiply values by</param>
    /// <param name="oldScale">
    ///     Inferus - the scale previously applied to the entity, so that re-applying a profile does not
    ///     compound hitbox and zoom changes. Defaults to an unscaled entity.
    /// </param>
    /// <returns>True if all operations succeeded</returns>
    public bool SetScale(EntityUid uid, Vector2 scale, Vector2? oldScale = null)
    {
        var succeeded = true;
        var avg = (scale.X + scale.Y) / 2;

        // Inferus - Start - scale relative to the previous scale instead of the current values
        var oldAvg = oldScale is { } old ? (old.X + old.Y) / 2 : 1f;
        if (oldAvg <= 0)
            oldAvg = 1f;
        var ratio = avg / oldAvg;
        // Inferus - End

        if (_config.GetCVar(CCVars.HeightAdjustModifiesZoom) && TryComp<ContentEyeComponent>(uid, out var eye))
            _eye.SetMaxZoom(uid, eye.MaxZoom * ratio); // Inferus - was avg
        else
            succeeded = false;

        if (_config.GetCVar(CCVars.HeightAdjustModifiesHitbox) && TryComp<FixturesComponent>(uid, out var fixtures))
        {
            foreach (var fixture in fixtures.Fixtures)
            {
                // Inferus - only circles have a meaningful radius, polygon radius is just their skin
                if (fixture.Value.Shape is not PhysShapeCircle)
                    continue;

                _physics.SetRadius(uid, fixture.Key, fixture.Value, fixture.Value.Shape, MathF.MinMagnitude(fixture.Value.Shape.Radius * ratio, 0.49f), fixtures); // Inferus - was avg
            }
        }
        else
            succeeded = false;

        // Inferus - was _appearance.SetScale(uid, scale) on the humanoid appearance, Inferus scales the sprite through ScaleVisuals
        if (MathHelper.CloseTo(scale.X, 1f) && MathHelper.CloseTo(scale.Y, 1f) && !HasComp<ScaleVisualsComponent>(uid))
        {
            // no need to add ScaleVisuals to an unscaled entity
        }
        else
            _scaleVisuals.SetSpriteScale(uid, scale);

        RaiseLocalEvent(uid, new HeightAdjustedEvent { NewScale = scale });

        return succeeded;
    }
}
