using Microsoft.AspNetCore.Identity;

namespace QRQueue.Models
{
    public class ApplicationRole:IdentityRole<Guid>
    {
        public ApplicationRole():base()
        {
            Id = Guid.NewGuid();
        }
        public ApplicationRole(string roleName) : this()
        {
            Name = roleName;
        }
        public List<Authority> Authorities { get; set; } =new List<Authority>();
    }
}
