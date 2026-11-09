using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client.Humanoid;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Inferus.Markings;

[TestFixture]
public sealed class LayeredMarkingTest
{
    private const string Chain = "InferusColorChainTest";
    private const string Cycle = "InferusColorCycleTest";
    private static readonly ProtoId<MarkingPrototype> Leopard = "TailLeopard";

    [TestPrototypes]
    private const string Prototypes = @"
- type: marking
  id: InferusColorChainTest
  bodyPart: Tail
  layering:
    afro: TailBehind
    afro2: TailBehindBackpack
  colorLinks:
    afro: afro2
    afro2: antenna
  sprites:
  - {sprite: Mobs/Customization/human_hair.rsi, state: afro}
  - {sprite: Mobs/Customization/human_hair.rsi, state: afro2}
  - {sprite: Mobs/Customization/human_hair.rsi, state: antenna}

- type: marking
  id: InferusColorCycleTest
  bodyPart: Hair
  colorLinks:
    afro: afro2
    afro2: afro
    antenna: missing
  sprites:
  - {sprite: Mobs/Customization/human_hair.rsi, state: afro}
  - {sprite: Mobs/Customization/human_hair.rsi, state: afro2}
  - {sprite: Mobs/Customization/human_hair.rsi, state: antenna}

- type: marking
  id: InferusLayeredHeadTest
  bodyPart: HeadTop
  groupWhitelist: [Moth]
  layering:
    m_ears_deer_ADJ: TailBehind
  colorLinks:
    m_ears_deer_ADJ: m_ears_deer_FRONT
  canBeDisplaced: false
  sprites:
  - {sprite: _Floof/Mobs/Customization/skyratears.rsi, state: m_ears_deer_ADJ}
  - {sprite: _Floof/Mobs/Customization/skyratears.rsi, state: m_ears_deer_FRONT}
";

    [Test]
    public async Task LinkedColorsResolveChainsAndSafelyHandleInvalidLinks()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        await pair.Server.WaitAssertion(() =>
        {
            var chain = pair.Server.ProtoMan.Index<MarkingPrototype>(Chain);
            var colors = new[] { Color.Red, Color.Green, Color.Blue };
            for (var i = 0; i < chain.Sprites.Count; i++)
            {
                Assert.That(chain.GetColorIndex(i), Is.EqualTo(2));
                Assert.That(chain.GetSpriteColor(colors, i), Is.EqualTo(Color.Blue));
            }

            Assert.That(chain.GetSpriteLayer(chain.Sprites[0]), Is.EqualTo(HumanoidVisualLayers.TailBehind));
            Assert.That(chain.GetSpriteLayer(chain.Sprites[1]), Is.EqualTo(HumanoidVisualLayers.TailBehindBackpack));
            Assert.That(chain.GetSpriteLayer(chain.Sprites[2]), Is.EqualTo(HumanoidVisualLayers.Tail));
            Assert.That(chain.GetSpriteColor(null, 0), Is.EqualTo(Color.White));
            Assert.That(chain.GetSpriteColor(new[] { Color.Red }, 0), Is.EqualTo(Color.White));

            var cycle = pair.Server.ProtoMan.Index<MarkingPrototype>(Cycle);
            for (var i = 0; i < cycle.Sprites.Count; i++)
            {
                Assert.That(cycle.GetColorIndex(i), Is.EqualTo(i));
                Assert.That(cycle.GetSpriteColor(colors, i), Is.EqualTo(colors[i]));
            }
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task LegacyMothPalettesArePreservedAndMappingIsIdempotent()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        await pair.Server.WaitAssertion(() =>
        {
            var manager = pair.Server.ResolveDependency<MarkingManager>();
            var prototypes = pair.Server.ProtoMan.EnumeratePrototypes<MarkingPrototype>()
                .Where(proto => proto.LegacyColorIndices != null).ToList();
            Assert.That(prototypes, Has.Count.EqualTo(23));

            foreach (var proto in prototypes)
            {
                var indices = proto.LegacyColorIndices!;
                var oldColors = new[] { Color.Red, Color.Green, Color.Blue, Color.Yellow }
                    .Take(indices.Values.Max() + 1).ToArray();
                var markings = new Dictionary<HumanoidVisualLayers, List<Marking>>
                {
                    [proto.BodyPart] = new() { new(proto.ID, oldColors) { Forced = true } },
                };
                manager.EnsureValidColors(markings);
                var mapped = markings[proto.BodyPart][0];
                Assert.That(mapped.Forced, Is.True, proto.ID);
                Assert.That(mapped.MarkingColors, Has.Count.EqualTo(proto.Sprites.Count), proto.ID);

                for (var i = 0; i < proto.Sprites.Count; i++)
                {
                    var state = MarkingPrototype.GetSpriteName(proto.Sprites[proto.GetColorIndex(i)])!;
                    var expected = indices.TryGetValue(state, out var oldIndex) ? oldColors[oldIndex] : Color.White;
                    Assert.That(mapped.MarkingColors[i], Is.EqualTo(expected), $"{proto.ID}/{state}");
                }

                manager.EnsureValidColors(markings);
                Assert.That(markings[proto.BodyPart][0], Is.EqualTo(mapped), proto.ID);
            }
        });
        await pair.CleanReturnAsync();
    }

    [TestCase("Human", "TailPede", "Torso", HumanoidVisualLayers.Tail)]
    [TestCase("Moth", "MothWingsDefault", "Torso", HumanoidVisualLayers.Tail)]
    [TestCase("Moth", "MothBeetleTail", "Torso", HumanoidVisualLayers.Tail)]
    [TestCase("Moth", "InferusLayeredHeadTest", "Head", HumanoidVisualLayers.HeadTop)]
    public async Task SplitLayersRenderInOrderAndAreRemovedTogether(
        string species, string markingId, string organ, HumanoidVisualLayers bodyPart)
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;
        await client.WaitAssertion(() =>
        {
            var entities = client.EntMan;
            var sprites = entities.System<SpriteSystem>();
            var visuals = entities.System<SharedVisualBodySystem>();
            var proto = client.ProtoMan.Index<MarkingPrototype>(markingId);
            var doll = entities.SpawnEntity(client.ProtoMan.Index<SpeciesPrototype>(species).DollPrototype, MapCoordinates.Nullspace);
            var marking = new Marking(markingId, Enumerable.Range(0, proto.Sprites.Count)
                .Select(i => i % 2 == 0 ? Color.Red : Color.Blue));

            visuals.ApplyMarkings(doll, new()
            {
                [organ] = new() { [bodyPart] = new() { marking } },
            });

            var sprite = entities.GetComponent<SpriteComponent>(doll);
            Assert.That(sprites.LayerMapGet(doll, HumanoidVisualLayers.TailBehind),
                Is.LessThan(sprites.LayerMapGet(doll, HumanoidVisualLayers.Chest)));
            Assert.That(sprites.LayerMapGet(doll, HumanoidVisualLayers.TailBehindBackpack),
                Is.GreaterThan(sprites.LayerMapGet(doll, "outerClothing")));
            Assert.That(sprites.LayerMapGet(doll, HumanoidVisualLayers.TailBehindBackpack),
                Is.LessThan(sprites.LayerMapGet(doll, "back")));

            var previousBySlot = new Dictionary<HumanoidVisualLayers, int>();
            for (var i = 0; i < proto.Sprites.Count; i++)
            {
                var state = MarkingPrototype.GetSpriteName(proto.Sprites[i]);
                var layer = sprites.LayerMapGet(doll, $"{markingId}-{state}");
                var slot = proto.GetSpriteLayer(proto.Sprites[i]);
                Assert.That(layer, Is.GreaterThan(sprites.LayerMapGet(doll, slot)), state);
                if (previousBySlot.TryGetValue(slot, out var previous))
                    Assert.That(layer, Is.GreaterThan(previous), state);
                previousBySlot[slot] = layer;
                Assert.That(sprite[layer].Color, Is.EqualTo(proto.GetSpriteColor(marking.MarkingColors, i)), state);
            }

            if (bodyPart == HumanoidVisualLayers.HeadTop)
            {
                var hiding = entities.System<SharedHideableHumanoidLayersSystem>();
                foreach (var hidden in new[] { true, false })
                {
                    hiding.SetLayerOcclusion(doll, bodyPart, hidden, SlotFlags.HEAD);
                    foreach (var state in proto.Sprites.Select(MarkingPrototype.GetSpriteName))
                        Assert.That(sprite[sprites.LayerMapGet(doll, $"{markingId}-{state}")].Visible, Is.EqualTo(!hidden));
                }
            }

            visuals.ApplyMarkings(doll, new()
            {
                [organ] = new() { [bodyPart] = new() },
            });
            foreach (var state in proto.Sprites.Select(MarkingPrototype.GetSpriteName))
                Assert.That(sprites.LayerMapTryGet(doll, $"{markingId}-{state}", out _, false), Is.False);
            entities.DeleteEntity(doll);
        });
        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task CharacterEditorShowsOnlyIndependentColorsAndEditsTheirOriginalIndices()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings { Connected = true });
        var client = pair.Client;
        LayerMarkingItem item = null!;
        Button button = null!;
        MarkingsViewModel model = null!;
        await client.WaitAssertion(() =>
        {
            var manager = client.ResolveDependency<MarkingManager>();
            model = new MarkingsViewModel
            {
                OrganData = manager.GetMarkingData("Human"),
                OrganProfileData = manager.GetProfileData("Human", Sex.Male, Color.White, Color.White),
            };
            model.ValidateMarkings();
            Assert.That(model.TrySelectMarking("Torso", HumanoidVisualLayers.Tail, "TailLeopard"), Is.True);
            item = new LayerMarkingItem(model, "Torso", HumanoidVisualLayers.Tail,
                client.ProtoMan.Index(Leopard), true);
            button = item.FindControl<Button>("ColorsButton");
            button.Mode = BaseButton.ActionMode.Press;
        });
        await client.DoGuiEvent(button, new GUIBoundKeyEventArgs(
            EngineKeyFunctions.UIClick, BoundKeyState.Down, default, default, Vector2.Zero, Vector2.Zero));
        await client.DoGuiEvent(button, new GUIBoundKeyEventArgs(
            EngineKeyFunctions.UIClick, BoundKeyState.Up, default, default, Vector2.Zero, Vector2.Zero));

        await client.WaitAssertion(() =>
        {
            var colors = item.FindControl<BoxContainer>("ColorsContainer");
            Assert.That(colors.ChildCount, Is.EqualTo(4));
            Assert.That(colors.GetChild(0).Visible, Is.True);
            Assert.That(colors.GetChild(1).Visible, Is.True);
            Assert.That(colors.GetChild(2).Visible, Is.False);
            Assert.That(colors.GetChild(3).Visible, Is.False);
            var slider = (ColorSelectorSliders) colors.GetChild(1).GetChild(1);
            slider.Color = Color.Blue;
            slider.OnColorChanged?.Invoke(Color.Blue);
            var marking = model.GetMarking("Torso", HumanoidVisualLayers.Tail, "TailLeopard")!.Value;
            var proto = client.ProtoMan.Index(Leopard);
            Assert.That(marking.MarkingColors[1], Is.EqualTo(Color.Blue));
            Assert.That(proto.GetSpriteColor(marking.MarkingColors, 3), Is.EqualTo(Color.Blue));
            item.Dispose();
        });
        await pair.CleanReturnAsync();
    }
}
