using Content.Shared._Lavaland.Aggression;
using Content.Shared._Lavaland.Megafauna.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Inferus.Lavaland;

/// <summary>
/// Megafauna used to keep running its AI after its last aggressor died or was deleted,
/// then crashed debug servers trying to pick a target from an empty aggressor list.
/// </summary>
[TestFixture]
public sealed class MegafaunaAggressorTest
{
    [Test]
    public async Task MegafaunaStopsWhenLastAggressorIsGone()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var map = await pair.CreateTestMap();

        EntityUid boss = default;
        EntityUid human = default;

        await server.WaitPost(() =>
        {
            boss = entMan.SpawnEntity("MobHierophant", map.GridCoords);
            human = entMan.SpawnEntity("MobHuman", map.GridCoords.Offset(new System.Numerics.Vector2(3, 0)));
            entMan.System<AggressorsSystem>().AddAggressor((boss, entMan.GetComponent<AggressiveComponent>(boss)), human);
        });

        await pair.RunSeconds(3);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<MegafaunaAiComponent>(boss).Active, Is.True);
            entMan.DeleteEntity(human);
        });

        // Would previously throw from AggressivePickTargetSelector on the next AI tick.
        await pair.RunSeconds(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.GetComponent<AggressiveComponent>(boss).Aggressors, Is.Empty);
            Assert.That(entMan.GetComponent<MegafaunaAiComponent>(boss).Active, Is.False);
            entMan.DeleteEntity(boss);
        });

        await pair.CleanReturnAsync();
    }
}
