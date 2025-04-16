using Micon.LotterySystem.Models;
using Micon.LotterySystem.Models.API;
using Micon.LotterySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Micon.LotterySystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController(RoleManager<ApplicationRole> roleManager,IAuthorityScanService authorityScanService,ApplicationDbContext applicationDbContext) : ControllerBase
    {
        [Authorize(Policy = "RoleManagement")]
        [HttpPost(nameof(CreateRole))]
        public async Task<IActionResult> CreateRole([FromBody] string roleName)
        {
            var role = await roleManager.FindByNameAsync(roleName);
            if (role != null)
            {
                return BadRequest("Role already exists");
            }
            var result = await roleManager.CreateAsync(new ApplicationRole(roleName));
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            return Ok();
        }

        [Authorize(Policy = "RoleManagement")]
        [HttpPost(nameof(DeleteRole))]
        public async Task<IActionResult> DeleteRole([FromBody] string roleName)
        {
            if(roleName.ToLower() == "admin")
            {
                return Conflict();
            }
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                return NotFound();
            }
            
            var result = await roleManager.DeleteAsync(role);
            applicationDbContext.RemoveRange(role.Authorities);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            return Ok(applicationDbContext.Authorities.ToArray());
        }

        [Authorize]
        [HttpGet(nameof(RoleList))]
        public async Task<IActionResult> RoleList()
        {
            var roles = await roleManager.Roles.Include(x => x.Authorities)
                .Select(x=>new SendRole(x))
                .ToListAsync();
            return Ok(roles);
        }
        [Authorize]
        [HttpGet(nameof(GetRole))]
        public async Task<IActionResult> GetRole([FromQuery] string roleName)
        {
            var role = await roleManager.Roles.Where(x=>x.Name == roleName).Include(x=>x.Authorities).FirstOrDefaultAsync();
            if (role == null)
            {
                return NotFound();
            }
            SendRole sendRole = new SendRole(role);
            return Ok(sendRole);
        }

        [Authorize(Policy = "RoleManagement")]
        [HttpGet(nameof(AuthorityList))]
        public async Task<IActionResult> AuthorityList()
        {
            var authority = authorityScanService.Authority;
            return Ok(authority);
        }
        [Authorize(Policy = "RoleManagement")]
        [HttpPost(nameof(AddAuthority))]
        public async Task<IActionResult> AddAuthority([FromBody] RoleAuthority roleAuthority)
        {
            var role = await roleManager.Roles.Where(x => x.Name == roleAuthority.RoleName)
                .Include(x => x.Authorities).FirstOrDefaultAsync();

            if (!authorityScanService.Authority.Contains(roleAuthority.Authority))
            {
                return NotFound();
            }
            if (role == null)
            {
                return NotFound();
            }
            if(role.Authorities.Any(x=>x.Name == roleAuthority.Authority))
            {
                return Conflict();
            }
            var authority = new Authority() { Name = roleAuthority.Authority };
            applicationDbContext.Add(authority);
            role.Authorities.Add(authority);
            applicationDbContext.Update(role);
            await applicationDbContext.SaveChangesAsync();
            return Ok();
        }
        [Authorize(Policy = "RoleManagement")]
        [HttpPost(nameof(RemoveAuthority))]
        public async Task<IActionResult> RemoveAuthority([FromBody] RoleAuthority roleAuthority)
        {
            var role = await roleManager.Roles.Where(x => x.Name == roleAuthority.RoleName)
                .Include(x => x.Authorities).FirstOrDefaultAsync();

            if (!authorityScanService.Authority.Contains(roleAuthority.Authority))
            {
                return NotFound();
            }
            if (role == null)
            {
                return NotFound();
            }
            var authority = role.Authorities.FirstOrDefault(x => x.Name == roleAuthority.Authority);
            if (authority == null)
            {
                return Conflict();
            }
            role.Authorities.Remove(authority);
            applicationDbContext.Authorities.Remove(authority);
            applicationDbContext.Update(role);
            await applicationDbContext.SaveChangesAsync();
            return Ok();
        }

    }
}
