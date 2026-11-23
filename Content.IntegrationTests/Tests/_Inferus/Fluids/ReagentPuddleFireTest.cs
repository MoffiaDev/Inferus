#nullable enable
using Content.Server._Funkystation.Atmos.Events;
using Content.Server._Funkystation.ReagentFires.Components;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Inferus.Fluids;

/// <summary>
/// Flammable puddles (e.g. welding fuel) used to never ignite because the puddle solution change
/// was relayed through an event nobody listened to, so the puddle never got its fire component.
/// </summary>
[TestFixture]
[TestOf(typeof(ReagentPuddleFireComponent))]
public sealed class ReagentPuddleFireTest
{
    [Test]
    public async Task FuelPuddleIgnites()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var map = await pair.CreateTestMap();
        var puddleSys = entMan.System<PuddleSystem>();

        EntityUid puddle = default;

        await server.WaitAssertion(() =>
        {
            var solution = new Solution("WeldingFuel", FixedPoint2.New(30));
            var (x, y) = map.Tile.GridIndices;
            var coords = new EntityCoordinates(map.Grid.Owner, x, y);
            Assert.That(puddleSys.TrySpillAt(coords, solution, out puddle), Is.True);
        });

        await pair.RunTicksSync(5);

        await server.WaitAssertion(() =>
        {
            Assert.That(entMan.TryGetComponent(puddle, out ReagentPuddleFireComponent? fire), Is.True,
                "A welding fuel puddle should be flammable");
            Assert.That(fire!.Flammability, Is.GreaterThan(0));
            Assert.That(fire.OnFire, Is.False);

            // Same event a lit welder or spark raises through atmos HotspotExpose.
            var ev = new TileExposedEvent(map.Tile.GridIndices, 700f, 5f, null);
            entMan.EventBus.RaiseLocalEvent(map.Grid.Owner, ref ev);

            Assert.That(fire.OnFire, Is.True, "A welding fuel puddle should ignite when exposed to a hot spark");
            entMan.DeleteEntity(puddle);
        });

        await pair.CleanReturnAsync();
    }
}
