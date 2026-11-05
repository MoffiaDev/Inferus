using System.Linq;
using System.Text;
using System.Threading;
using Content.Shared._Inferus.CCVar;
using Content.Shared._Inferus.Speech;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private readonly List<SpeechBlipPrototype> _speechBlips = new();
    private CancellationTokenSource? _speechBlipPreviewCancel;

    private void InitializeSpeechBlip()
    {
        SpeechBlipButton.OnItemSelected += args =>
        {
            SpeechBlipButton.SelectId(args.Id);
            SetSpeechBlip(_speechBlips[args.Id]);
            PlaySpeechBlipPreview();
        };

        SpeechBlipPreviewButton.OnPressed += _ => PlaySpeechBlipPreview();
    }

    private void UpdateSpeechBlipControls()
    {
        if (Profile == null)
            return;

        SpeechBlipButton.Clear();
        _speechBlips.Clear();

        _speechBlips.AddRange(_prototypeManager.EnumeratePrototypes<SpeechBlipPrototype>()
            .OrderBy(blip => blip.Order)
            .ThenBy(blip => Loc.GetString(blip.Name)));

        for (var i = 0; i < _speechBlips.Count; i++)
        {
            SpeechBlipButton.AddItem(Loc.GetString(_speechBlips[i].Name), i);

            if (Profile.SpeechBlip == _speechBlips[i].ID)
                SpeechBlipButton.SelectId(i);
        }

        UpdateSpeechBlipPreviewButton();
    }

    private void SetSpeechBlip(ProtoId<SpeechBlipPrototype> blip)
    {
        Profile = Profile?.WithSpeechBlip(blip);
        SetDirty();
        UpdateSpeechBlipPreviewButton();
    }

    private void UpdateSpeechBlipPreviewButton()
    {
        SpeechBlipPreviewButton.Disabled = Profile == null
            || !_prototypeManager.TryIndex(Profile.SpeechBlip, out var blip)
            || !blip.Typewriter
            || _entManager.System<SpeechBlipSystem>().GetClips(blip).Count == 0;
    }

    /// <summary>
    /// Plays the blips for a short sample line, as if it were being typed out.
    /// </summary>
    private void PlaySpeechBlipPreview()
    {
        _speechBlipPreviewCancel?.Cancel();
        _speechBlipPreviewCancel = null;

        if (Profile == null
            || !_prototypeManager.TryIndex(Profile.SpeechBlip, out var blip)
            || !blip.Typewriter)
        {
            return;
        }

        var gain = _cfgManager.GetCVar(InferusCCVars.SpeechBlipVolume);
        if (gain <= 0f)
            return;

        var audio = _entManager.System<SharedAudioSystem>();
        var speechBlip = _entManager.System<SpeechBlipSystem>();
        var interval = MathF.Max(blip.CharacterDelay, blip.BlipCooldown);
        var sequence = 0;
        var last = -1;
        var time = 0f;

        _speechBlipPreviewCancel = new CancellationTokenSource();
        foreach (var letter in Loc.GetString("humanoid-profile-editor-speech-blip-preview-text").EnumerateRunes())
        {
            if (!Rune.IsLetterOrDigit(letter))
            {
                time += interval;
                continue;
            }

            if (speechBlip.PickClip(blip, letter, ref sequence, ref last) is not { } clip)
                return;

            var audioParams = clip.Params.WithVolume(clip.Params.Volume + SharedAudioSystem.GainToVolume(gain));
            Timer.Spawn(TimeSpan.FromSeconds(time),
                () => audio.PlayGlobal(clip.Sound, Filter.Local(), false, audioParams),
                _speechBlipPreviewCancel.Token);

            time += interval;
        }
    }
}
