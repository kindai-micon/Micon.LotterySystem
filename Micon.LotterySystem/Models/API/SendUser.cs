namespace Micon.LotterySystem.Models.API
{
    public class SendUser
    {
        public SendUser(ApplicationUser applicationUser)
        {
            UserName = applicationUser.UserName;
        }
        public string UserName { get; set; }
        public List<SendRole> Roles { get; set; } = new List<SendRole>();
    }

    public class SendRole
    {
        public SendRole(ApplicationRole applicationRole)
        {
            Name = applicationRole.Name;
            if(applicationRole.Authorities == null) return;
            foreach (var authority in applicationRole.Authorities)
            {
                Authorities.Add(new SendAuthority(authority));
            }
        }
        public string Name { get; set; }
        public List<SendAuthority> Authorities { get; set; } = new List<SendAuthority>();
    }

    public class SendAuthority
    {
        public SendAuthority(Authority authority)
        {
            Name = authority.Name;
        }
        public string Name { get; set; }
    }
}
