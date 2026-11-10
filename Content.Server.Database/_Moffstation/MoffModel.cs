using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public static class MoffModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MoffPreference>()
            .HasOne(p => p.Preference)
            .WithOne(p => p.MoffPreference)
            .HasForeignKey<MoffPreference>(p => p.PreferenceId)
            .IsRequired();

        modelBuilder.Entity<MoffJobPriority>()
            .HasOne(p => p.MoffPreference)
            .WithMany(p => p.JobPriorities)
            .HasForeignKey(p => p.MoffPreferenceId)
            .IsRequired();

        modelBuilder.Entity<MoffJobPriority>()
            .HasIndex(p => new { p.MoffPreferenceId, p.JobName })
            .IsUnique();

        modelBuilder.Entity<MoffProfile>()
            .HasOne(p => p.Profile)
            .WithOne(p => p.MoffProfile)
            .HasForeignKey<MoffProfile>(p => p.ProfileId)
            .IsRequired();
    }

    public class MoffPreference
    {
        public int Id { get; set; }
        public int PreferenceId { get; set; }
        public Preference Preference { get; set; } = null!;
        public List<MoffJobPriority> JobPriorities { get; } = new();
    }

    public class MoffJobPriority
    {
        public int Id { get; set; }
        public int MoffPreferenceId { get; set; }
        public MoffPreference MoffPreference { get; set; } = null!;
        [Required] public string JobName { get; set; } = null!;
        public DbJobPriority Priority { get; set; }
    }

    public class MoffProfile
    {
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public Profile Profile { get; set; } = null!;
        public bool Enabled { get; set; } = true;
    }
}

