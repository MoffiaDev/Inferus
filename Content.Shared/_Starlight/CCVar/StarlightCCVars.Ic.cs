using Robust.Shared.Configuration;

namespace Content.Shared._Starlight.CCVar;

public sealed partial class StarlightCCVars
{
    /// <summary>
    /// Restricts IC character custom specie names so they cannot be others species.
    /// </summary>
    public static readonly CVarDef<bool> RestrictedCustomSpecieNames =
        CVarDef.Create("ic.restricted_customspecienames", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Whether or not players can open character inspect windows on other players
    /// </summary>
    public static readonly CVarDef<bool> CharacterInspectWindowEnabled =
        CVarDef.Create("ic.inspect_windows", true, CVar.SERVER | CVar.REPLICATED); // Inferus: was false
}
