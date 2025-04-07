using Micon.LotterySystem.Models;
using Microsoft.EntityFrameworkCore;

namespace Micon.LotterySystem
{
    public class ApplicationDbContext:DbContext
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
    }
}
