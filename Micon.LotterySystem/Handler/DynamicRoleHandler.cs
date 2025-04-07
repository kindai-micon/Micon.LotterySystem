using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Micon.LotterySystem.Handler
{
    public class DynamicRoleHandler(ApplicationDbContext dbContext) : AuthorizationHandler<DynamicRoleRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DynamicRoleRequirement requirement)
        {
            var authorities = dbContext.Authorities.Include(a => a.Role)
                .ToList();
            foreach(var authority in authorities)
            {
                if (context.User.IsInRole(authority.Role.Name))
                {

                    context.Succeed(requirement);
                    break;
                }
            }

            return Task.CompletedTask;
        }
    }
}
