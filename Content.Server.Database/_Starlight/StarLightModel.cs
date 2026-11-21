// SPDX-FileCopyrightText: 2025 Starlight
// SPDX-License-Identifier: Starlight-MIT

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

public static class StarLightModel
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StarLightProfile>(entity =>
        {
            entity.HasOne(e => e.Profile)
                .WithOne(p => p.StarLightProfile)
                .HasForeignKey<StarLightProfile>(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.ProfileId)
                .IsUnique();

            entity.Property(e => e.CustomSpecieName)
                .HasMaxLength(32);
        });

        modelBuilder.Entity<CharacterInfo>(entity =>
        {
            entity.HasOne(e => e.Profile)
                .WithOne(p => p.CharacterInfo)
                .HasForeignKey<CharacterInfo>(e => e.ProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public class StarLightProfile
    {
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public virtual Profile Profile { get; set; } = null!;
        public string? CustomSpecieName { get; set; }
        // Inferus - ForcedPrototype, CyberneticIds, Width and Height are not ported.
    }

    [Table("sl_character_info")]
    public partial class CharacterInfo
    {
        [Key, ForeignKey("Profile")]
        public int ProfileId { get; set; }

        public virtual Profile Profile { get; set; } = null!;

        // Inferus - no PhysicalDesc column; the physical description is stored in Profile.FlavorText.

        [MaxLength(4096)]
        public string PersonalityDesc { get; set; } = string.Empty;

        [MaxLength(4096)]
        public string PersonalNotes { get; set; } = string.Empty;

        [MaxLength(4096)]
        public string OOCNotes { get; set; } = string.Empty;
    }
}
