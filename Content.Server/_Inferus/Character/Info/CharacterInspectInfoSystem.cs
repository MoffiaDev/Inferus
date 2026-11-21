using Content.Shared._Inferus.Character.Info;
using Content.Shared._Starlight.Character.Info.Components;
using Content.Shared.Examine;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind;

namespace Content.Server._Inferus.Character.Info;

/// <summary>
/// Sends the mind-held character info (personality description and OOC notes) of a character
/// to a client that opened the Starlight character inspect window for them.
/// </summary>
public sealed class CharacterInspectInfoSystem : EntitySystem
{
    [Dependency] private ExamineSystemShared _examine = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestCharacterInspectInfoEvent>(OnRequest);
    }

    private void OnRequest(RequestCharacterInspectInfoEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } user
            || !TryGetEntity(ev.Target, out var target)
            || !HasComp<HumanoidProfileComponent>(target))
            return;

        // Same rules as the examine verb that opens the window: the character has to be recognisable and close enough.
        if (Identity.Name(target.Value, EntityManager) != MetaData(target.Value).EntityName
            || !_examine.IsInDetailsRange(user, target.Value))
            return;

        var personality = string.Empty;
        var oocNotes = string.Empty;
        if (_mind.GetMind(target.Value) is { } mind)
        {
            if (TryComp(mind, out CharacterDescriptionComponent? description))
                personality = description.Description;

            if (TryComp(mind, out RoleplayInfoComponent? roleplayInfo))
                oocNotes = roleplayInfo.OOCNotes;
        }

        RaiseNetworkEvent(new CharacterInspectInfoEvent(ev.Target, personality, oocNotes), args.SenderSession);
    }
}
