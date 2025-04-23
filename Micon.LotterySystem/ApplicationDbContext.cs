using Micon.LotterySystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Micon.LotterySystem
{
    public class ApplicationDbContext:IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public DbSet<LotteryGroup> LotteryGroups { get; set; }
        public DbSet<LotterySlots> LotterySlots { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Authority> Authorities { get; set; }
        public DbSet<IssueLog> IssueLogs { get; set; }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public ApplicationDbContext() : base()
        {
        }
    }
}
