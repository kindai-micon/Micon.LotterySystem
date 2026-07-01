using Microsoft.AspNetCore.Authorization;

namespace QRQueue.Handler
{
    public class DynamicRoleRequirement : IAuthorizationRequirement
    {
        public string Authority { get; }
        public DynamicRoleRequirement(string authority) => Authority = authority;
    }

}
