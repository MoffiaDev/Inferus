using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

/// <summary>
/// Container types for Cosmatic Drift profile data stored alongside the base profile tables.
/// </summary>
public static class CDModel
{
    // Inferus - moved the relationship setup here from ServerDbContext.OnModelCreating, like MoffModel.
    public static void Configure(ModelBuilder modelBuilder)
    {
        // Cosmatic Drift Record System-start: Link CD profile tables into the EF model so they persist with preferences
        modelBuilder.Entity<CDProfile>()
            .HasOne(p => p.Profile)
            .WithOne(p => p.CDProfile)
            .HasForeignKey<CDProfile>(p => p.ProfileId)
            .IsRequired();

        modelBuilder.Entity<CharacterRecordEntry>()
            .HasOne(e => e.CDProfile)
            .WithMany(e => e.CharacterRecordEntries)
            .HasForeignKey(e => e.CDProfileId)
            .IsRequired();
        // Cosmatic Drift Record System-end
    }

    public class CDProfile
    {
        public int Id { get; set; }

        public int ProfileId { get; set; }
        public Profile Profile { get; set; } = null!;

        [Column("character_records", TypeName = "jsonb")]
        public JsonDocument? CharacterRecords { get; set; }

        public List<CharacterRecordEntry> CharacterRecordEntries { get; set; } = new();
    }

    public enum DbRecordEntryType : byte
    {
        Medical = 0,
        Security = 1,
        Employment = 2,
        Admin = 3,
    }

    [Table("cd_character_record_entries"), Index(nameof(Id))]
    public sealed class CharacterRecordEntry
    {
        public int Id { get; set; }

        public string Title { get; set; } = null!;

        public string Involved { get; set; } = null!;

        public string Description { get; set; } = null!;

        public DbRecordEntryType Type { get; set; }

        public int CDProfileId { get; set; }
        public CDProfile CDProfile { get; set; } = null!;
    }
}
