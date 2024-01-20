#nullable disable
using Microsoft.EntityFrameworkCore;

namespace OOTRTruthSeedBot.DAL.Models;

public partial class Context : DbContext
{
    public Context(DbContextOptions<Context> options)
        : base(options)
    {
    }

    public virtual DbSet<Seed> Seeds { get; set; }

    public virtual DbSet<Version> Versions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Seed>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.ToTable("seed");

            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasColumnName("id");
            entity.Property(e => e.InternalCreatorId).HasColumnName("creator_id");
            entity.Property(e => e.InternalCreationDate).HasColumnName("creation_date");
            entity.Property(e => e.InternalUnlockedDate).HasColumnName("unlocked_date");
            entity.Property(e => e.InternalState).HasColumnName("state");

            entity.Ignore(e => e.CreatorId);
            entity.Ignore(e => e.CreationDate);
            entity.Ignore(e => e.UnlockedDate);
            entity.Ignore(e => e.IsGenerated);
            entity.Ignore(e => e.IsUnlocked);
            entity.Ignore(e => e.IsDeleted);
        });

        modelBuilder.Entity<Version>(entity =>
        {
            entity.HasKey(e => e.Current);

            entity.ToTable("version");

            entity.Property(e => e.Current).HasColumnName("current");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}