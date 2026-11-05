using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._Inferus.CCVar;

/// <summary>
/// Inferus-specific CVars.
/// </summary>
[CVarDefs]
public sealed class InferusCCVars : CVars
{
    /// <summary>
    /// If true, the old pain/crit overlays are used instead of the new shader-based ones.
    /// </summary>
    public static readonly CVarDef<bool> LegacyDamageOverlays =
        CVarDef.Create("inferus.legacy_damage_overlays", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// If true, other characters are glitched/censored by a sprite shader while the local player is in crit.
    /// </summary>
    public static readonly CVarDef<bool> CritGlitchOthers =
        CVarDef.Create("inferus.crit_glitch_others", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// If true, speech bubbles always appear instantly instead of typing out letter by letter.
    /// </summary>
    public static readonly CVarDef<bool> InstantSpeechBubbles =
        CVarDef.Create("inferus.instant_speech_bubbles", false, CVar.CLIENT | CVar.REPLICATED | CVar.ARCHIVE);

    /// <summary>
    /// Volume multiplier for speech blips. 0 mutes them.
    /// </summary>
    public static readonly CVarDef<float> SpeechBlipVolume =
        CVarDef.Create("inferus.speech_blip_volume", 1f, CVar.CLIENTONLY | CVar.ARCHIVE);
}
