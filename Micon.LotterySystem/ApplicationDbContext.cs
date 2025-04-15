using Micon.LotterySystem.Models;
using Microsoft.EntityFrameworkCore;

namespace Micon.LotterySystem
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<LotteryGroup> LotteryGroups { get; set; }
        public DbSet<LotterySlots> LotterySlots { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Authority> Authorities { get; set; }
        public DbSet<ApplicationRole> ApplicationRoles { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public ApplicationDbContext() : base()
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<LotteryGroup>(builder =>
            {
                builder.HasOne(x => x.TicketInfo)
                    .WithOne(x => x.LotteryGroup)
                    .HasForeignKey<LotteryGroup>(x => x.TicketInfoId);
            });

            modelBuilder.Entity<LotterySlots>(builder =>
            {
                builder.HasOne(x => x.LotteryGroup)
                    .WithMany(x => x.LotterySlots)
                    .HasForeignKey(x => x.LotteryGroupId);
            });

            modelBuilder.Entity<Ticket>(builder =>
            {
                builder.HasOne(x => x.LotteryGroup)
                    .WithMany(x => x.Tickets)
                    .HasForeignKey(x => x.LotteryGroupId);
                builder.HasOne(x => x.LotterySlots)
                    .WithMany(x => x.Tickets)
                    .HasForeignKey(x => x.LotterySlotsId);
            });

            modelBuilder.Entity<Authority>(builder =>
            {
                builder.HasOne(x => x.Role)
                    .WithMany(x => x.Authorities)
                    .HasForeignKey(x => x.RoleId);
            });
        }
    }
}
