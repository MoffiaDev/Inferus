using Content.Shared.Clothing.Components;
using Content.Shared.Inventory.Events;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared._Goobstation.Clothing.Systems;

// Inferus
public sealed partial class ClothingGrantingSystem : EntitySystem
{
    [Dependency] private IComponentFactory _componentFactory = default!;
    [Dependency] private ISerializationManager _serializationManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Components.ClothingGrantComponentComponent, GotEquippedEvent>(OnCompEquip);
        SubscribeLocalEvent<Components.ClothingGrantComponentComponent, GotUnequippedEvent>(OnCompUnequip);
    }

    private void OnCompEquip(EntityUid uid, Components.ClothingGrantComponentComponent component, GotEquippedEvent args)
    {
        if (!TryComp<ClothingComponent>(uid, out var clothing))
            return;

        if (!clothing.Slots.HasFlag(args.SlotFlags))
            return;

        foreach (var (name, data) in component.Components)
        {
            var newComp = (Component) _componentFactory.GetComponent(name);

            if (EntityManager.HasComponent(args.EquipTarget, newComp.GetType()))
                continue;

            var temp = (object) newComp;
            _serializationManager.CopyTo(data.Component, ref temp);
            EntityManager.AddComponent(args.EquipTarget, (Component) temp!);

            component.Active[name] = true;
        }
    }

    private void OnCompUnequip(EntityUid uid, Components.ClothingGrantComponentComponent component, GotUnequippedEvent args)
    {
        foreach (var (name, data) in component.Components)
        {
            if (!component.Active.ContainsKey(name) || !component.Active[name])
                continue;

            var newComp = (Component) _componentFactory.GetComponent(name);

            EntityManager.RemoveComponent(args.EquipTarget, newComp.GetType());
            component.Active[name] = false;
        }
    }
}
