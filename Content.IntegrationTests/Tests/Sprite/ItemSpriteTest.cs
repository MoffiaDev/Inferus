#nullable enable
using System.Collections.Generic;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Item;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests.Sprite;

/// <summary>
/// This test checks that all items have a visible sprite. The general rationale is that all items can be picked up
/// by players, thus they need to be visible and have a sprite that can be rendered on screen and in their hands GUI.
/// This has nothing to do with in-hand sprites.
/// </summary>
/// <remarks>
/// If a prototype fails this test, its probably either because it:
/// - Should be marked abstract
/// - inherits from BaseItem despite not being an item
/// - Shouldn't have an item component
/// - Is missing the required sprite information.
/// If none of the abveo are true, it might need to be added to the list of ignored components, see
/// <see cref="Ignored"/>
/// </remarks>
[TestFixture]
public sealed class PrototypeSaveTest : GameTest
{
    // Inferus: body-based items also need a synchronized server/client rendering check
    public override PoolSettings PoolSettings => new()
    {
        Connected = true,
        Dirty = true
    };

    private static readonly HashSet<string> Ignored = new()
    {
        // The only prototypes that should get ignored are those that REQUIRE setup to get a sprite. At that point it is
        // the responsibility of the spawner to ensure that a valid sprite is set.
        "VirtualItem",
        "DetachedBody"
    };

    [Test]
    public async Task AllItemsHaveSpritesTest()
    {
        var pair = Pair;
        List<EntityPrototype> badPrototypes = [];

        await pair.Client.WaitPost(() =>
        {
            foreach (var (proto, _) in pair.GetPrototypesWithComponent<ItemComponent>(Ignored))
            {
                var dummy = pair.Client.EntMan.Spawn(proto.ID);
                pair.Client.EntMan.RunMapInit(dummy, pair.Client.MetaData(dummy));
                var spriteComponent = pair.Client.EntMan.GetComponentOrNull<SpriteComponent>(dummy);
                if (!HasVisibleSprite(spriteComponent)) // Inferus: inspect all renderable layers
                    badPrototypes.Add(proto);
                pair.Client.EntMan.DeleteEntity(dummy);
            }
        });

        // Inferus: retry initialized bodies through the normal networked rendering path, without exempting them
        var bodyPrototypes = badPrototypes.FindAll(proto => proto.Components.ContainsKey("InitialBody"));
        if (bodyPrototypes.Count > 0)
        {
            var map = await pair.CreateTestMap();
            foreach (var proto in bodyPrototypes)
            {
                EntityUid serverEntity = default;
                NetEntity netEntity = default;
                await pair.Server.WaitPost(() =>
                {
                    serverEntity = pair.Server.EntMan.SpawnEntity(proto.ID, map.GridCoords);
                    netEntity = pair.Server.EntMan.GetNetEntity(serverEntity);
                });

                try
                {
                    await pair.RunUntilSynced();
                    await pair.Client.WaitPost(() =>
                    {
                        var clientEntity = pair.Client.EntMan.GetEntity(netEntity);
                        var sprite = pair.Client.EntMan.GetComponentOrNull<SpriteComponent>(clientEntity);
                        if (HasVisibleSprite(sprite)) // Inferus: inspect the initialized body layers
                            badPrototypes.Remove(proto);
                    });
                }
                finally
                {
                    await pair.Server.WaitPost(() =>
                    {
                        if (pair.Server.EntMan.EntityExists(serverEntity))
                            pair.Server.EntMan.DeleteEntity(serverEntity);
                    });
                    await pair.RunUntilSynced();
                }
            }
        }

        Assert.Multiple(() =>
        {
            foreach (var proto in badPrototypes)
            {
                Assert.Fail($"Item prototype has no sprite: {proto.ID}. It should probably either be marked as abstract, not be an item, or have a valid sprite");
            }
        });
    }

    // Inferus: a layered sprite may have blank leading slots; require actual visible texture/state data
    private static bool HasVisibleSprite(SpriteComponent? sprite)
    {
        if (sprite == null)
            return false;

        foreach (var layer in sprite.AllLayers)
        {
            if (!layer.Visible)
                continue;

            if (layer.Texture != null)
                return true;

            if (!layer.RsiState.IsValid)
                continue;

            var rsi = layer.Rsi ?? sprite.BaseRSI;
            if (rsi != null && rsi.TryGetState(layer.RsiState, out _))
                return true;
        }

        return false;
    }
}
