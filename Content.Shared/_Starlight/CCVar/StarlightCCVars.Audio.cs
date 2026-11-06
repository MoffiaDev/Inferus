using Robust.Shared.Configuration;

namespace Content.Shared._Starlight.CCVar;

public sealed partial class StarlightCCVars
{
    /// <summary>
    /// How strongly rooms echo, scales every room reverb (roomReverb prototypes).
    /// </summary>
    public static readonly CVarDef<float> ReverbVolume =
        CVarDef.Create("audio.reverb_volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
