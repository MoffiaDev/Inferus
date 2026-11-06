using System.Numerics;
using System.Text;
using Content.Shared._Inferus.CCVar;
using Content.Shared._Inferus.Speech;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Chat.UI;

/// <summary>
/// Typewriter effect for speech bubbles. Reveals the bubble's text one letter at a time,
/// playing the speaker's <see cref="SpeechBlipPrototype"/> clips as letters appear.
/// </summary>
public abstract partial class SpeechBubble
{
    [Dependency] private IPrototypeManager _prototype = default!;

    /// <summary>
    /// Characters that halt the typewriter for <see cref="SpeechBlipPrototype.PauseDelay"/> seconds.
    /// </summary>
    private const string TypewriterPauseCharacters = ",.!?-~";

    /// <summary>
    /// How much quieter blips are when whispering, in decibels.
    /// </summary>
    private const float WhisperBlipVolume = -6f;

    private RichTextLabel? _typewriterLabel;
    private FormattedMessage? _typewriterMessage;
    private SpeechBlipPrototype? _typewriterBlip;
    private bool _typewriterWhisper;
    private bool _typewriterBreaksFound;

    private Rune[] _typewriterRunes = Array.Empty<Rune>();
    private float[] _typewriterTimes = Array.Empty<float>();
    private float[] _typewriterPitches = Array.Empty<float>();
    private readonly HashSet<int> _typewriterBreaks = new();

    private int _typewriterRevealed;
    private float _typewriterElapsed;
    private float _typewriterLastBlip = float.MinValue;
    private int _typewriterClipSequence;
    private int _typewriterLastClip = -1;

    /// <summary>
    /// How many seconds it takes for this bubble to finish typing out. 0 if it appears instantly.
    /// </summary>
    public float TypewriterDuration { get; private set; }

    /// <summary>
    /// True while there are still letters left to reveal.
    /// </summary>
    public bool IsTypewriting => _typewriterBlip != null && _typewriterRevealed < _typewriterRunes.Length;

    /// <summary>
    /// Marks the label that should be typed out. Call from <see cref="BuildBubble"/> after setting its message.
    /// </summary>
    protected void SetTypewriterTarget(RichTextLabel label, FormattedMessage message)
    {
        _typewriterLabel = label;
        _typewriterMessage = message;
    }

    /// <summary>
    /// Sets up the typewriter. Must be called after the bubble has been styled, but before it is measured.
    /// </summary>
    private void InitializeTypewriter(string speechStyleClass)
    {
        if (_typewriterLabel == null || _typewriterMessage == null)
            return;

        if (ConfigManager.GetCVar(InferusCCVars.InstantSpeechBubbles))
            return;

        if (!_entityManager.TryGetComponent<SpeechBlipComponent>(_senderEntity, out var blipComp)
            || !_prototype.TryIndex(blipComp.Blip, out var blip)
            || !blip.Typewriter)
        {
            return;
        }

        var runes = new List<Rune>();
        foreach (var node in _typewriterMessage)
        {
            if (node.Name != null || !node.Value.TryGetString(out var text))
                continue;

            foreach (var rune in text.EnumerateRunes())
            {
                runes.Add(rune);
            }
        }

        if (runes.Count == 0)
            return;

        _typewriterBlip = blip;
        _typewriterWhisper = speechStyleClass == "whisperBox";
        _typewriterRunes = runes.ToArray();
        _typewriterTimes = new float[_typewriterRunes.Length];

        var time = 0f;
        for (var i = 0; i < _typewriterRunes.Length; i++)
        {
            _typewriterTimes[i] = time;
            time += blip.CharacterDelay;

            if (IsTypewriterPause(i))
                time += blip.PauseDelay;
        }

        TypewriterDuration = _typewriterTimes[^1];

        _typewriterPitches = new float[_typewriterRunes.Length];
        CalculateTypewriterPitches(blip.IntonationPitch);

        LockTypewriterSize();
        _typewriterLabel.SetMessage(new FormattedMessage(), tagsAllowed: null);
    }

    /// <summary>
    /// Locks the label to the size of the full message so the bubble doesn't grow as letters appear.
    /// </summary>
    private void LockTypewriterSize()
    {
        if (_typewriterLabel == null)
            return;

        _typewriterLabel.MinSize = Vector2.Zero;
        _typewriterLabel.Measure(Vector2Helpers.Infinity);
        _typewriterLabel.MinSize = _typewriterLabel.DesiredSize
            - new Vector2(_typewriterLabel.Margin.SumHorizontal, _typewriterLabel.Margin.SumVertical);
    }

    /// <summary>
    /// Works out the pitch of each letter, sentence by sentence.
    /// Questions start at the normal pitch and rise, exclamations arc up towards the middle and fall back down by the end.
    /// </summary>
    private void CalculateTypewriterPitches(float intonation)
    {
        var start = 0;
        var length = _typewriterRunes.Length;

        for (var i = 0; i < length; i++)
        {
            // A sentence ends after a run of . ! or ? that's followed by a space, or at the end of the message.
            var atEnd = i == length - 1;
            if (!atEnd && !(IsSentenceEnd(_typewriterRunes[i]) && Rune.IsWhiteSpace(_typewriterRunes[i + 1])))
                continue;

            var question = false;
            var exclaim = false;
            for (var j = i; j >= start && IsSentenceEnd(_typewriterRunes[j]); j--)
            {
                question |= _typewriterRunes[j].Value == '?';
                exclaim |= _typewriterRunes[j].Value == '!';
            }

            // Shouting counts as exclaiming, same as the regular speech sounds.
            if (!question && !exclaim)
                exclaim = IsMostlyUppercase(start, i);

            for (var j = start; j <= i; j++)
            {
                var progress = i > start ? (float) (j - start) / (i - start) : 1f;

                if (question)
                    _typewriterPitches[j] = 1f + intonation * progress;
                else if (exclaim)
                    _typewriterPitches[j] = 1f + intonation * MathF.Sin(progress * MathF.PI);
                else
                    _typewriterPitches[j] = 1f;
            }

            start = i + 1;
        }
    }

    private static bool IsSentenceEnd(Rune rune)
    {
        return rune.Value is '.' or '!' or '?';
    }

    private bool IsMostlyUppercase(int start, int end)
    {
        var letters = 0;
        var upper = 0;
        for (var i = start; i <= end; i++)
        {
            if (!Rune.IsLetter(_typewriterRunes[i]))
                continue;

            letters++;
            if (Rune.IsUpper(_typewriterRunes[i]))
                upper++;
        }

        return letters > 1 && upper > letters / 2;
    }

    /// <summary>
    /// Pauses only happen at the end of a run of punctuation that's followed by a space,
    /// so things like "...", "?!", "3.14" or "well-known" don't stutter.
    /// </summary>
    private bool IsTypewriterPause(int index)
    {
        if (!IsTypewriterPauseCharacter(_typewriterRunes[index]))
            return false;

        if (index + 1 >= _typewriterRunes.Length)
            return false;

        return Rune.IsWhiteSpace(_typewriterRunes[index + 1]);
    }

    private static bool IsTypewriterPauseCharacter(Rune rune)
    {
        return rune.IsAscii && TypewriterPauseCharacters.Contains((char) rune.Value);
    }

    private void UpdateTypewriter(float frameTime)
    {
        if (!IsTypewriting || _typewriterLabel == null)
            return;

        // Done here instead of on creation, since the label needs to be in the UI tree to know the real UI scale.
        if (!_typewriterBreaksFound)
        {
            FindTypewriterLineBreaks();
            _typewriterBreaksFound = true;
        }

        _typewriterElapsed += frameTime;

        var revealed = _typewriterRevealed;
        while (revealed < _typewriterRunes.Length && _typewriterTimes[revealed] <= _typewriterElapsed)
        {
            revealed++;
        }

        if (revealed == _typewriterRevealed)
            return;

        var blipIndex = -1;
        for (var i = _typewriterRevealed; i < revealed; i++)
        {
            if (Rune.IsLetterOrDigit(_typewriterRunes[i]))
                blipIndex = i;
        }

        _typewriterRevealed = revealed;

        if (_typewriterRevealed >= _typewriterRunes.Length)
        {
            _typewriterLabel.SetMessage(_typewriterMessage!, tagsAllowed: null);
        }
        else
        {
            _typewriterLabel.SetMessage(BuildTypewriterMessage(_typewriterRevealed, true), tagsAllowed: null);
        }

        if (blipIndex >= 0)
            TryPlayBlip(blipIndex);
    }

    /// <summary>
    /// Instantly reveals the rest of the message.
    /// </summary>
    public void FinishTypewriter()
    {
        if (!IsTypewriting || _typewriterLabel == null)
            return;

        _typewriterRevealed = _typewriterRunes.Length;
        _typewriterLabel.SetMessage(_typewriterMessage!, tagsAllowed: null);
    }

    private void TryPlayBlip(int index)
    {
        if (_typewriterBlip == null)
            return;

        if (_typewriterElapsed - _typewriterLastBlip < _typewriterBlip.BlipCooldown)
            return;

        var gain = ConfigManager.GetCVar(InferusCCVars.SpeechBlipVolume);
        if (gain <= 0f)
            return;

        var speechBlip = _entityManager.System<SpeechBlipSystem>();
        if (speechBlip.PickClip(_typewriterBlip, _typewriterRunes[index], ref _typewriterClipSequence, ref _typewriterLastClip) is not { } clip)
            return;

        _typewriterLastBlip = _typewriterElapsed;

        var audioParams = clip.Params;
        audioParams = audioParams.WithVolume(audioParams.Volume + SharedAudioSystem.GainToVolume(gain));

        if (_typewriterWhisper)
            audioParams = audioParams.WithVolume(audioParams.Volume + WhisperBlipVolume);

        audioParams = audioParams.WithPitchScale(audioParams.Pitch * _typewriterPitches[index]);

        _entityManager.System<SharedAudioSystem>().PlayEntity(clip.Sound, Filter.Local(), _senderEntity, false, audioParams);
    }

    /// <summary>
    /// Works out where the full message word wraps, so partially typed text can use explicit line breaks
    /// instead of words jumping down a line halfway through being typed.
    /// </summary>
    private void FindTypewriterLineBreaks()
    {
        if (_typewriterLabel == null)
            return;

        // Measure without the size lock getting in the way.
        _typewriterLabel.MinSize = Vector2.Zero;

        var lastHeight = -1f;
        var wordStart = -1;

        for (var i = 0; i <= _typewriterRunes.Length; i++)
        {
            var atEnd = i == _typewriterRunes.Length;
            var isSpace = !atEnd && Rune.IsWhiteSpace(_typewriterRunes[i]);

            if (!atEnd && !isSpace)
            {
                if (wordStart < 0)
                    wordStart = i;

                continue;
            }

            if (wordStart < 0)
                continue;

            _typewriterLabel.SetMessage(BuildTypewriterMessage(i, false), tagsAllowed: null);
            _typewriterLabel.Measure(Vector2Helpers.Infinity);
            var height = _typewriterLabel.DesiredSize.Y;

            // The line count went up, so this word wrapped onto a new line.
            if (lastHeight >= 0f && height > lastHeight + 0.5f && wordStart > 0)
            {
                var previous = _typewriterRunes[wordStart - 1];
                if (Rune.IsWhiteSpace(previous) && previous.Value != '\n')
                    _typewriterBreaks.Add(wordStart - 1);
            }

            lastHeight = height;
            wordStart = -1;
        }

        // Re-lock the size now that the real UI scale is known.
        _typewriterLabel.SetMessage(_typewriterMessage!, tagsAllowed: null);
        LockTypewriterSize();

        _typewriterLabel.SetMessage(BuildTypewriterMessage(_typewriterRevealed, true), tagsAllowed: null);
    }

    /// <summary>
    /// Builds a copy of the message containing only the first <paramref name="count"/> characters.
    /// </summary>
    private FormattedMessage BuildTypewriterMessage(int count, bool withBreaks)
    {
        var msg = new FormattedMessage();
        if (_typewriterMessage == null || count <= 0)
            return msg;

        var index = 0;
        var builder = new StringBuilder();

        foreach (var node in _typewriterMessage)
        {
            if (node.Name != null)
            {
                msg.PushTag(node);
                continue;
            }

            if (!node.Value.TryGetString(out var text))
                continue;

            builder.Clear();
            foreach (var rune in text.EnumerateRunes())
            {
                if (index >= count)
                    break;

                if (withBreaks && _typewriterBreaks.Contains(index))
                    builder.Append('\n');
                else
                    builder.Append(rune.ToString());

                index++;
            }

            if (builder.Length > 0)
                msg.AddText(builder.ToString());

            if (index >= count)
                break;
        }

        return msg;
    }
}
