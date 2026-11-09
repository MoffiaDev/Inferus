using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._ES.Spawning;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Spawning;

/// <summary>
/// Handles late-join requests from the spawn menu.
/// </summary>
/// <remarks>
/// Mirrors the checks done by <see cref="Content.Server.GameTicking.Commands.JoinGameCommand"/>.
/// </remarks>
public sealed partial class ESSpawningSystem : EntitySystem
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private GameTicker _gameTicker = default!;
    [Dependency] private StationJobsSystem _stationJobs = default!;

    // Inferus: use server-owned profiles and the existing explicit-choice spawn path
    [Dependency] private Content.Server.Preferences.Managers.IServerPreferencesManager _characterPreferences = default!;
    [Dependency] private Content.Server._Moffstation.Station.MoffCharacterPickerSystem _characterPicker = default!;

    private ISawmill _securitySawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _securitySawmill = LogManager.GetSawmill("security");

        SubscribeNetworkEvent<ESSpawnPlayerEvent>(OnSpawnPlayer);
    }

    private void OnSpawnPlayer(ESSpawnPlayerEvent msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession;

        if (_gameTicker.PlayerGameStatuses.TryGetValue(player.UserId, out var status) && status == PlayerGameStatus.JoinedGame)
        {
            _securitySawmill.Info($"{player.Name} ({player.UserId}) attempted to latejoin while in-game.");
            return;
        }

        if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
            return;

        if (!_prototype.TryIndex(msg.JobId, out var job))
            return;

        var stations = new List<EntityUid>();
        var query = EntityQueryEnumerator<StationJobsComponent>();
        while (query.MoveNext(out var station, out _))
        {
            if (!_stationJobs.TryGetJobSlot(station, job, out var slots) || slots == 0)
                continue;

            stations.Add(station);
        }

        if (stations.Count == 0)
            return;

        // Inferus: reject stale/forged slots instead of silently rolling another character
        if (msg.CharacterSlot is not { } slot ||
            _characterPreferences.GetPreferencesOrNull(player.UserId) is not { } preferences ||
            !preferences.Characters.TryGetValue(slot, out var profile))
            return;

        if (_admin.IsAdmin(player) && _config.GetCVar(CCVars.AdminDeadminOnJoin))
            _admin.DeAdmin(player);

        // Inferus: pin only this request; clean up even when spawning exits early
        _characterPicker.SetExplicitChoice(player.UserId, profile);
        try
        {
            _gameTicker.MakeJoinGame(player, _random.Pick(stations), job.ID);
        }
        finally
        {
            _characterPicker.ClearExplicitChoice(player.UserId);
        }
    }
}
