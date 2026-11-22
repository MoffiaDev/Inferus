using System.Numerics;
using Content.Server._Funkystation.SistrCore; // funky
using Content.Server._MACRO.Announcements;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Components; // funky
using Content.Server.Station.Systems;
using Content.Server.StationEvents.Components;
using Content.Shared._Funkystation.CCVar;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Random.Helpers;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that spawn meteors, throwing them at the largest grid of the station.
/// </summary>
/// <seealso cref="MeteorSwarmComponent"/>
public sealed partial class MeteorSwarmSystem : GameRuleSystem<MeteorSwarmComponent>
{
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private ServerStationSystem _station = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private ChatSystem _chat = default!;
    [Dependency] private SistrCoreSystem _sistrCore = default!;
    [Dependency] private IConfigurationManager _cfg = null!; // funky - pa announcement cvar

    [Dependency] private AnnouncerManager _announcer = default!; // Macrocosm edit

    protected override void Added(Entity<MeteorSwarmComponent, GameRuleComponent> rule, ref GameRuleAddedEvent args)
    {
        base.Added(rule, ref args);
        var stations = _station.GetStations();
        if (stations.Count == 0)
            return;

        var station = RobustRandom.Pick(stations);
        if (_station.GetLargestGrid(station.AsNullable()) is not { } grid)
            return;

        rule.Comp1.TargetGrid = grid;
        rule.Comp1.WaveCounter = rule.Comp1.Waves.Next(RobustRandom);

        var allPlayersInGame = _station.GetOnMap(station.Comp); // Inferus: upstream now only announces to the station's map
        var canAnnounce = _sistrCore.ShouldStationEventBeAnnounced(rule); // Inferus: was uid

        // Macrocosm edit start - announcer variation
        SoundSpecifier? sound = null; // funky

        var paExclusive = PAAnnouncementCVars.IsPAEnabledAndExclusive(_cfg); // funky

        if (canAnnounce && rule.Comp1.AnnouncementSound is { } soundId && _announcer.TryGetAnnouncerSound(soundId, out sound)) // funky
        {
            if (!paExclusive) // funky
                _audio.PlayGlobal(sound, allPlayersInGame, true);
        }
        // Macrocosm edit end

        if (canAnnounce && rule.Comp1.Announcement is { } locId)
        {
            _chat.DispatchFilteredAnnouncement(allPlayersInGame, Loc.GetString(locId),
                sender: Loc.GetString(Comp<StationEventComponent>(rule).StartAnnouncementSender), // funky
                playSound: paExclusive, announcementSound: paExclusive ? sound : null, // funky
                colorOverride: Comp<StationEventComponent>(rule).StartAnnouncementColor);
        }
    }

    protected override void ActiveTick(EntityUid uid, MeteorSwarmComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (Timing.CurTime < component.NextWaveTime)
            return;

        if (component.TargetGrid is not { } grid)
        {
            ForceEndSelf((uid, gameRule));
            return;
        }

        component.NextWaveTime += TimeSpan.FromSeconds(component.WaveCooldown.Next(RobustRandom));

        var mapId = Transform(grid).MapID;
        var playableArea = _physics.GetWorldAABB(grid);

        var minimumDistance = (playableArea.TopRight - playableArea.Center).Length() + 50f;
        var maximumDistance = minimumDistance + 100f;

        var center = playableArea.Center;

        IRobustRandom random;
        if (component.NonDirectional)
        {
            random = RobustRandom;
        }
        else
        {
            random = new RobustRandom();
            random.SetSeed(uid.Id);
        }

        var meteorsToSpawn = component.MeteorsPerWave.Next(RobustRandom);
        for (var i = 0; i < meteorsToSpawn; i++)
        {
            var spawnProto = RobustRandom.Pick(component.Meteors);

            var angle = random.NextAngle();

            var offset = angle.RotateVec(new Vector2((maximumDistance - minimumDistance) * RobustRandom.NextFloat() + minimumDistance, 0));

            // the line at which spawns occur is perpendicular to the offset.
            // This means the meteors are less likely to bunch up and hit the same thing.
            var subOffsetAngle = RobustRandom.Prob(0.5f)
                ? angle + Math.PI / 2
                : angle - Math.PI / 2;
            var subOffset = subOffsetAngle.RotateVec(new Vector2((playableArea.TopRight - playableArea.Center).Length() / 3 * RobustRandom.NextFloat(), 0));

            var spawnPosition = new MapCoordinates(center + offset + subOffset, mapId);
            var meteor = Spawn(spawnProto, spawnPosition);
            var physics = Comp<PhysicsComponent>(meteor);
            _physics.ApplyLinearImpulse(meteor, -offset.Normalized() * component.MeteorVelocity * physics.Mass, body: physics);
        }

        component.WaveCounter--;
        if (component.WaveCounter <= 0)
        {
            ForceEndSelf((uid, gameRule));
        }
    }
}
