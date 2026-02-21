using System.ComponentModel.DataAnnotations;

namespace Micon.LotterySystem.Models
{
    public class RefreshToken
    {
        public RefreshToken()
        {
            Id = Guid.CreateVersion7();
        }

        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        public Guid UserId { get; set; }

        public ApplicationUser User { get; set; } = null!;

        public DateTimeOffset ExpiresAt { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;

        public bool IsRevoked { get; set; }
    }
}
