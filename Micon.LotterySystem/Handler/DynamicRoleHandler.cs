using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Micon.LotterySystem.Handler
{
    public class DynamicRoleHandler(ApplicationDbContext dbContext) : AuthorizationHandler<DynamicRoleRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, DynamicRoleRequirement requirement)
        {
            var roles = dbContext.Authorities.Include(a => a.Role)
                .ToList();
            foreach(var role in roles)
            {
                if (context.User.IsInRole(role.Name))
                {

                    context.Succeed(requirement);
                    break;
                }
            }

            return Task.CompletedTask;
        }
    }
}
