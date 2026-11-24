using System;
using System.Threading.Tasks;
using Content.IntegrationTests.Fixtures;
using Content.IntegrationTests.Fixtures.Attributes;
using Content.Server.Kitchen.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared._Inferus.Kitchen;
using Content.Shared.Kitchen.Components;
using Robust.Shared.Containers;

namespace Content.IntegrationTests.Tests._Inferus.Kitchen;

public sealed class MicrowaveReactivityTest : GameTest
{
    [SidedDependency(Side.Server)] private readonly MicrowaveReactivitySystem _reactivity = null!;
    [SidedDependency(Side.Server)] private readonly ServerMicrowaveSystem _microwave = null!;
    [SidedDependency(Side.Server)] private readonly SharedContainerSystem _container = null!;

    [TestPrototypes]
    private const string Prototypes = @"
- type: entity
  id: InferusMicrowaveReactivityTestItem
  abstract: true
  components:
  - type: Item

- type: entity
  id: InferusMicrowaveReactivityTestMetal
  parent: InferusMicrowaveReactivityTestItem
  components:
  - type: PhysicalComposition
    materialComposition:
      Steel: 10

- type: entity
  id: InferusMicrowaveReactivityTestAlloy
  parent: InferusMicrowaveReactivityTestItem
  components:
  - type: PhysicalComposition
    materialComposition:
      InferusMicrowaveReactivityTestAlloyMaterial: 10

- type: material
  id: InferusMicrowaveReactivityTestAlloyMaterial
  parent: Steel

- type: entity
  id: InferusMicrowaveReactivityTestElectronic
  parent: InferusMicrowaveReactivityTestItem
  components:
  - type: Circuitboard
  - type: PhysicalComposition
    materialComposition:
      Glass: 10

- type: entity
  id: InferusMicrowaveReactivityTestId
  parent: InferusMicrowaveReactivityTestItem
  components:
  - type: IdCard
    canMicrowave: false
  - type: Circuitboard
  - type: PhysicalComposition
    materialComposition:
      Steel: 10
  - type: Tag
    tags:
    - Metal
    - Plastic

- type: entity
  id: InferusMicrowaveReactivityTestFood
  parent: InferusMicrowaveReactivityTestItem
  components:
  - type: PhysicalComposition
    materialComposition:
      Meaterial: 10

- type: entity
  id: InferusMicrowaveReactivityTestZeroMetal
  parent: InferusMicrowaveReactivityTestItem
  components:
  - type: PhysicalComposition
    materialComposition:
      Steel: 0

- type: entity
  id: InferusMicrowaveReactivityTestOven
  parent: KitchenMicrowave
  components:
  - type: Microwave
    cookTimeMultiplier: 1
    explosionChance: 0
    lightningChance: 0
";

    [TestCase("InferusMicrowaveReactivityTestMetal", true)]
    [TestCase("InferusMicrowaveReactivityTestAlloy", true)]
    [TestCase("InferusMicrowaveReactivityTestElectronic", true)]
    [TestCase("InferusMicrowaveReactivityTestId", false)]
    [TestCase("InferusMicrowaveReactivityTestFood", false)]
    [TestCase("InferusMicrowaveReactivityTestZeroMetal", false)]
    public async Task ClassifiesContents(string prototype, bool expected)
    {
        var item = await Spawn(prototype);
        await Server.WaitAssertion(() => Assert.That(_reactivity.IsReactive(item), Is.EqualTo(expected)));
    }

    [TestCase("InferusMicrowaveReactivityTestMetal", 0u, true)]
    [TestCase("InferusMicrowaveReactivityTestMetal", 5u, true)]
    [TestCase("InferusMicrowaveReactivityTestElectronic", 0u, true)]
    [TestCase("InferusMicrowaveReactivityTestElectronic", 5u, true)]
    [TestCase("InferusMicrowaveReactivityTestId", 0u, false)]
    [TestCase("InferusMicrowaveReactivityTestId", 5u, false)]
    [TestCase("InferusMicrowaveReactivityTestFood", 0u, false)]
    [TestCase("InferusMicrowaveReactivityTestFood", 5u, false)]
    public async Task CookingUsesClassification(string prototype, uint seconds, bool expected)
    {
        var map = await Pair.CreateTestMap();
        var uid = await SpawnAtPosition("InferusMicrowaveReactivityTestOven", map.GridCoords);
        await SpawnAtPosition("APCBasic", map.GridCoords);
        await Server.WaitRunTicks(1);
        var item = await Spawn(prototype);

        await Server.WaitAssertion(() =>
        {
            Assert.That(_microwave.IsPowered(uid, SEntMan), Is.True, "Test microwave must be powered.");
            var component = SComp<MicrowaveComponent>(uid);
            var microwave = (Owner: uid, Comp: component);
            _microwave.SetCookTime(microwave, seconds);
            Assert.That(_container.Insert(item, _container.GetContainer(uid, component.ContainerId)), Is.True);
            _microwave.StartCooking(microwave, null);

            var active = SComp<ActiveMicrowaveComponent>(uid);
            Assert.That(active.Malfunctioning, Is.EqualTo(expected));
            Assert.That(SEntMan.EntityExists(item), Is.True, "The ID exclusion must also bypass generic plastic burning.");
            if (!expected)
                return;

            var started = active.CookTimeEnd - TimeSpan.FromSeconds(seconds);
            var firstRoll = seconds == 0 ? started : started + component.MalfunctionInterval;
            Assert.That(active.NextMalfunction, Is.EqualTo(firstRoll));
        });

        await Pair.RunSeconds(seconds + 0.1f);
        await Server.WaitAssertion(() => Assert.That(_microwave.IsActiveMicrowave(uid), Is.False));
    }
}
