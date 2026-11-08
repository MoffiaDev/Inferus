using Robust.Shared.Configuration;

namespace Content.Shared._Goobstation.CCVar;

[CVarDefs]
public sealed partial class GoobCVars
{
    public static readonly CVarDef<bool> RoundIntroEnabled =
        CVarDef.Create("game.round_intro", true, CVar.CLIENTONLY | CVar.ARCHIVE);
}
