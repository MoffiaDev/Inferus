using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Database;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Configuration;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

namespace Content.IntegrationTests.Tests._Inferus.Preferences;

[TestFixture]
public sealed class MultiCharacterDatabaseTest : GameTest
{
    [TestCase(false)]
    [TestCase(true)]
    public async Task SeedsAllProfilesAndPersistsSelection(bool emptyPriorities)
    {
        var server = Pair.Server;
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<SqliteServerDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new ServerDbSqlite(
            () => options,
            true,
            server.ResolveDependency<IConfigurationManager>(),
            true,
            server.ResolveDependency<ILogManager>().GetSawmill("db.ops"),
            server.ResolveDependency<ISerializationManager>(),
            false);
        var user = new NetUserId(Guid.NewGuid());
        var first = new HumanoidCharacterProfile()
            .WithJobPriorities(new Dictionary<ProtoId<JobPrototype>, JobPriority>
            {
                ["Captain"] = JobPriority.High,
                ["Passenger"] = JobPriority.Low,
            })
            .WithTraitPreference("Unrevivable", server.ProtoMan);
        var selected = new HumanoidCharacterProfile()
            .WithJobPriorities(new Dictionary<ProtoId<JobPrototype>, JobPriority>
            {
                ["StationEngineer"] = JobPriority.High,
                ["Passenger"] = JobPriority.Medium,
            });

        // Normal preference creation must precede the multi-character load on first login.
        await db.InitPrefsAsync(user, first);
        await db.SaveCharacterSlotAsync(user, selected, 1);
        await db.SaveSelectedCharacterIndexAsync(user, 1);
        var seeded = await db.GetMoffCharacterSelectionAsync(user);
        Assert.Multiple(() =>
        {
            Assert.That(seeded.IsAuthoritative, Is.True);
            Assert.That(seeded.GetPriority("Captain"), Is.EqualTo(JobPriority.Medium));
            Assert.That(seeded.GetPriority("StationEngineer"), Is.EqualTo(JobPriority.High));
            Assert.That(seeded.GetPriority("Passenger"), Is.EqualTo(JobPriority.Medium));
            Assert.That(seeded.EnabledSlots.Keys, Is.EquivalentTo(new[] { 0, 1 }));
            Assert.That(seeded.IsSlotEnabled(0) && seeded.IsSlotEnabled(1), Is.True);
        });

        var priorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>();
        if (!emptyPriorities)
            priorities["Captain"] = JobPriority.High;

        await db.SaveMoffJobPrioritiesAsync(user, priorities);
        await db.SaveMoffCharacterEnabledAsync(user, 1, false);
        await db.SaveCharacterSlotAsync(user, selected.WithName("Updated Selection"), 1);
        var persisted = await db.GetMoffCharacterSelectionAsync(user);
        var preferences = await db.GetPlayerPreferencesAsync(user);
        Assert.Multiple(() =>
        {
            Assert.That(persisted.IsAuthoritative, Is.True);
            Assert.That(persisted.JobPriorities, Is.EquivalentTo(priorities));
            Assert.That(persisted.IsSlotEnabled(1), Is.False);
            Assert.That(preferences!.Profiles, Has.Count.EqualTo(2));
            Assert.That(preferences.Profiles.Single(profile => profile.Slot == 0).Traits
                .Select(trait => trait.TraitName), Does.Contain("Unrevivable"));
            Assert.That(preferences.Profiles.Single(profile => profile.Slot == 1).CharacterName,
                Is.EqualTo("Updated Selection"));
        });

        await db.SaveSelectedCharacterIndexAsync(user, 0);
        await db.SaveCharacterSlotAsync(user, null, 1);
        var afterDelete = await db.GetMoffCharacterSelectionAsync(user);
        Assert.That(afterDelete.EnabledSlots.Keys, Is.EquivalentTo(new[] { 0 }));
        Assert.That(afterDelete.JobPriorities, Is.EquivalentTo(priorities));
    }
}
