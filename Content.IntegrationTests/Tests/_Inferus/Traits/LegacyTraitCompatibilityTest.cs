using Content.Server._DV.Traits;
using Content.Shared._DV.Traits;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Inferus.Traits;

[TestFixture]
public sealed partial class LegacyTraitCompatibilityTest
{
    private const string LegacyTrait = "InferusLegacyTraitTest";
    private static readonly ProtoId<TraitPrototype> Muted = "Muted";
    private static readonly ProtoId<TraitCategoryPrototype> SpeechTraits = "SpeechTraits";

    [TestPrototypes]
    private const string Prototypes = @"
- type: trait
  id: InferusLegacyTraitTest
  name: trait-unrevivable-name
  description: trait-unrevivable-name
  components:
  - type: InferusLegacyTraitMarker
  traitGear: WhiteCane
";

    [RegisterComponent]
    private sealed partial class InferusLegacyTraitMarkerComponent : Component;

    [Test]
    public async Task LegacyFieldsAndUncategorizedTraitsStillWork()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entities = server.EntMan;

        await server.WaitAssertion(() =>
        {
            var prototype = server.ProtoMan.Index<TraitPrototype>(LegacyTrait);
            Assert.That(prototype.Category.Id, Is.EqualTo("Misc"));
            Assert.That(prototype.Cost, Is.Zero);
            Assert.That(server.ProtoMan.Index(SpeechTraits).MaxPoints, Is.EqualTo(2));
            Assert.That(server.ProtoMan.Index(Muted).Specials, Is.Not.Empty);

            var mob = entities.SpawnEntity("MobHuman", MapCoordinates.Nullspace);
            entities.System<TraitSystem>().ApplyTrait(mob, prototype);
            Assert.That(entities.HasComponent<InferusLegacyTraitMarkerComponent>(mob), Is.True);

            var hands = entities.GetComponent<HandsComponent>(mob);
            var cane = entities.System<SharedHandsSystem>().GetActiveItem((mob, hands));
            Assert.That(cane, Is.Not.Null);
            Assert.That(entities.GetComponent<MetaDataComponent>(cane!.Value).EntityPrototype?.ID, Is.EqualTo("WhiteCane"));
            entities.DeleteEntity(mob);
        });

        await pair.CleanReturnAsync();
    }
}
