using API.Contexts.Objects;
using Microsoft.EntityFrameworkCore;

// ReSharper disable PropertyCanBeMadeInitOnly.Global

namespace API.Contexts;

public class DbManager : DbContext
{
    public DbManager()
    {
    }

    public DbManager(DbContextOptions<DbManager> options) : base(options)
    {
    }

    public virtual DbSet<ArmorSale> ArmorSales { get; set; } = null!;
    public virtual DbSet<BungieProfile> BungieProfiles { get; set; } = null!;
    public virtual DbSet<LostSector> LostSectors { get; set; } = null!;
    public virtual DbSet<LostSectorLootPool> LostSectorLootPools { get; set; } = null!;
    public virtual DbSet<Metric> Metrics { get; set; } = null!;
    public virtual DbSet<Role> Roles { get; set; } = null!;
    public virtual DbSet<User> Users { get; set; } = null!;
    public virtual DbSet<UserBan> UserBans { get; set; } = null!;
    public virtual DbSet<UserRole> UserRoles { get; set; } = null!;
    public virtual DbSet<VendorUser> VendorUsers { get; set; } = null!;
    public virtual DbSet<WeaponSale> WeaponSales { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ArmorSale>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("armor_sale_pkey");

            entity.ToTable("armor_sale");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Class).HasColumnName("class");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.SetName).HasColumnName("set_name");
            entity.Property(e => e.Stats).HasColumnName("stats");
            entity.Property(e => e.VendorId).HasColumnName("vendor_id");
        });

        modelBuilder.Entity<BungieProfile>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("bungie_profile_pkey");

            entity.ToTable("bungie_profile");

            entity.HasIndex(e => e.MembershipId, "bungie_profile_membership_id_unique").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.MembershipId).HasColumnName("membership_id");
            entity.Property(e => e.NeverExpire).HasColumnName("never_expire");
            entity.Property(e => e.OauthToken).HasColumnName("oauth_token");
            entity.Property(e => e.RefreshExpires)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("refresh_expires");
            entity.Property(e => e.RefreshToken).HasColumnName("refresh_token");
            entity.Property(e => e.TokenExpires)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("token_expires");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<LostSector>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("lost_sector_pkey");

            entity.ToTable("lost_sector");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.ArcCount).HasColumnName("arc_count");
            entity.Property(e => e.BarrierCount).HasColumnName("barrier_count");
            entity.Property(e => e.OverloadCount).HasColumnName("overload_count");
            entity.Property(e => e.SolarCount).HasColumnName("solar_count");
            entity.Property(e => e.UnstopCount).HasColumnName("unstop_count");
            entity.Property(e => e.VoidCount).HasColumnName("void_count");
        });

        modelBuilder.Entity<LostSectorLootPool>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("lost_sector_lootpool_pkey");

            entity.ToTable("lost_sector_lootpool");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ItemIds).HasColumnName("item_ids");
        });

        modelBuilder.Entity<Metric>(entity =>
        {
            entity.HasKey(e => e.Slug).HasName("metric_pkey");

            entity.ToTable("metric");

            entity.Property(e => e.Slug).HasColumnName("slug");
            entity.Property(e => e.LastUse)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("last_use");
            entity.Property(e => e.TotalUse).HasColumnName("total_use");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Slug).HasName("role_pkey");

            entity.ToTable("role");

            entity.Property(e => e.Slug).HasColumnName("slug");
            entity.Property(e => e.BadgeUrl).HasColumnName("badge_url");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_pkey");

            entity.ToTable("user");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.LastCommand)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("last_command");
            entity.Property(e => e.RegisteredFelicity).HasColumnName("registered_felicity");
            entity.Property(e => e.RegisteredLostSector).HasColumnName("registered_lostsector");
        });

        modelBuilder.Entity<UserBan>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_ban_pkey");

            entity.ToTable("user_ban");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.BanReason).HasColumnName("ban_reason");
            entity.Property(e => e.BanTime)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("ban_time");

            entity.HasOne(d => d.User).WithOne(p => p.UserBan)
                .HasForeignKey<UserBan>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_ban_user_id_foreign");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("user_role_pkey");

            entity.ToTable("user_role");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.RoleSlug).HasColumnName("role_slug");

            entity.HasOne(d => d.RoleSlugNavigation).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleSlug)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_role_role_slug_foreign");

            entity.HasOne(d => d.User).WithOne(p => p.UserRole)
                .HasForeignKey<UserRole>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("user_role_user_id_foreign");
        });

        modelBuilder.Entity<VendorUser>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("vendor_user_pkey");

            entity.ToTable("vendor_user");

            entity.HasIndex(e => new { e.VendorId, e.Resets }, "vendor_user_vendor_id_resets_index");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .HasColumnName("user_id");
            entity.Property(e => e.Rank).HasColumnName("rank");
            entity.Property(e => e.Resets).HasColumnName("resets");
            entity.Property(e => e.VendorId).HasColumnName("vendor_id");

            entity.HasOne(d => d.User).WithOne(p => p.VendorUser)
                .HasForeignKey<VendorUser>(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("vendor_user_user_id_foreign");
        });

        modelBuilder.Entity<WeaponSale>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("weapon_sale_pkey");

            entity.ToTable("weapon_sale");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ItemId).HasColumnName("item_id");
            entity.Property(e => e.ItemPerks).HasColumnName("item_perks");
            entity.Property(e => e.QueryTime)
                .HasColumnType("timestamp(0) without time zone")
                .HasColumnName("query_time");
            entity.Property(e => e.RequiredRank).HasColumnName("required_rank");
            entity.Property(e => e.RequiredResets).HasColumnName("required_resets");
            entity.Property(e => e.VendorId).HasColumnName("vendor_id");
        });
    }
}
