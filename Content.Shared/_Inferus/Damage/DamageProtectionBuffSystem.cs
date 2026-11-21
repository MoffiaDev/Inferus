using Content.Shared.Damage;
using Content.Shared.Damage.Systems;

namespace Content.Shared._Inferus.Damage;

public sealed partial class DamageProtectionBuffSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DamageProtectionBuffComponent, DamageModifyEvent>(OnDamageModify);
    }

    private void OnDamageModify(EntityUid uid, DamageProtectionBuffComponent comp, DamageModifyEvent args)
    {
        foreach (var modifier in comp.Modifiers.Values)
        {
            args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifier);
        }
    }
}
