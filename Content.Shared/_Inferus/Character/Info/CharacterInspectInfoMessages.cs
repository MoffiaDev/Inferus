using Robust.Shared.Serialization;

namespace Content.Shared._Inferus.Character.Info;

/// <summary>
/// Sent by a client that opened the character inspect window, asking for the parts of the
/// target's character info that live on the target's mind. Minds are only networked to the
/// player controlling them, so other clients cannot read those components directly.
/// </summary>
[Serializable, NetSerializable]
public sealed class RequestCharacterInspectInfoEvent(NetEntity target) : EntityEventArgs
{
    public readonly NetEntity Target = target;
}

/// <summary>
/// Server reply to <see cref="RequestCharacterInspectInfoEvent"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class CharacterInspectInfoEvent(NetEntity target, string personalityDescription, string oocNotes)
    : EntityEventArgs
{
    public readonly NetEntity Target = target;
    public readonly string PersonalityDescription = personalityDescription;
    public readonly string OOCNotes = oocNotes;
}
