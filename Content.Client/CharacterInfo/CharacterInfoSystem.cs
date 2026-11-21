using Content.Shared.CharacterInfo;
using Content.Shared.Objectives;
using Content.Shared.Roles;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Prototypes;

namespace Content.Client.CharacterInfo;

public sealed partial class CharacterInfoSystem : EntitySystem
{
    [Dependency] private IPlayerManager _players = default!;

    public event Action<CharacterData>? OnCharacterUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CharacterInfoEvent>(OnCharacterInfoEvent);
    }

    public void RequestCharacterInfo()
    {
        var entity = _players.LocalEntity;
        if (entity == null)
        {
            return;
        }

        RaiseNetworkEvent(new RequestCharacterInfoEvent(GetNetEntity(entity.Value)));
    }

    private void OnCharacterInfoEvent(CharacterInfoEvent msg, EntitySessionEventArgs args)
    {
        // Inferus - the entity can be deleted or out of PVS by the time the reply arrives (e.g. after aghost or setmind)
        // var entity = GetEntity(msg.NetEntity);
        if (!TryGetEntity(msg.NetEntity, out var maybeEntity) || TerminatingOrDeleted(maybeEntity.Value))
            return;

        var entity = maybeEntity.Value;
        // End Inferus
        var data = new CharacterData(entity, msg.Objectives, msg.Briefing, msg.Job, Name(entity));

        OnCharacterUpdate?.Invoke(data);
    }

    public List<Control> GetCharacterInfoControls(EntityUid uid)
    {
        var ev = new GetCharacterInfoControlsEvent(uid);
        RaiseLocalEvent(uid, ref ev, true);
        return ev.Controls;
    }

    public readonly record struct CharacterData(
        EntityUid Entity,
        Dictionary<string, List<ObjectiveInfo>> Objectives,
        string? Briefing,
        ProtoId<JobPrototype>? JobId,
        string EntityName
    );

    /// <summary>
    /// Event raised to get additional controls to display in the character info menu.
    /// </summary>
    [ByRefEvent]
    public readonly record struct GetCharacterInfoControlsEvent(EntityUid Entity)
    {
        public readonly List<Control> Controls = new();

        public readonly EntityUid Entity = Entity;
    }
}
