using Microsoft.AspNetCore.Identity;

namespace Micon.LotterySystem.Models
{
    public class ApplicationUser:IdentityUser<Guid>
    {
        public ApplicationUser():base()
        {
            Id = Guid.CreateVersion7();

        }

        public ApplicationUser(string userName) : this()
        {
            UserName = userName;
        }

        public ICollection<RefreshToken> RefreshTokens { get; set; } = [];
    }
}
