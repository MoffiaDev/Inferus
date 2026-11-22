using Content.Server._Inferus.Construction;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Inferus.Construction;

/// <summary>
/// Checks that items placed into construction steps, machine frames and crafting recipes pick the right place sound.
/// </summary>
[TestFixture]
[TestOf(typeof(ConstructionInsertSoundSystem))]
public sealed class ConstructionInsertSoundTest
{
    private static readonly (string Prototype, string Rule)[] Cases =
    {
        ("SheetSteel1", "Metal"),
        ("PartRodMetal1", "Metal"),
        ("SheetGlass1", "Glass"),
        ("ShardGlass", "Glass"),
        ("CableApcStack1", "Wires"),
        ("DoorElectronics", "Electronics"),
        ("AutolatheMachineCircuitboard", "Electronics"),
        ("MicroManipulatorStockPart", "Item"),
        ("Crowbar", "Item"),
    };

    [Test]
    public async Task ItemsPickExpectedPlaceSound()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var map = await pair.CreateTestMap();
        var sys = entMan.System<ConstructionInsertSoundSystem>();

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                foreach (var (proto, rule) in Cases)
                {
                    var ent = entMan.SpawnEntity(proto, map.GridCoords);
                    Assert.That(sys.GetRule(ent)?.ID, Is.EqualTo(rule), $"{proto} should use the {rule} place sound");
                    entMan.DeleteEntity(ent);
                }
            });
        });

        await pair.CleanReturnAsync();
    }
}
