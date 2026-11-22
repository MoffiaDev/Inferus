using Content.Shared._Lavaland.Megafauna.Components;
using Content.Shared.Damage;
using Robust.Shared.Player;
using Content.Shared.Damage.Components; // Inferus
using Content.Shared.Damage.Systems; // Inferus

namespace Content.Shared._Lavaland.Megafauna.Systems;

public sealed class MegafaunaGodmodeSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MegafaunaGodmodeComponent, BeforeDamageChangedEvent>(OnBeforeDamageChanged);
    }

    private void OnBeforeDamageChanged(Entity<MegafaunaGodmodeComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (args.Origin == null
            || !HasComp<ActorComponent>(args.Origin))
            args.Cancelled = true;
    }
}
