using Robust.Shared.GameStates;

namespace Content.Shared._Goobstation.MartialArts.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class StaminaResistanceModifierStatusEffectComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Modifier = 1f;
}
