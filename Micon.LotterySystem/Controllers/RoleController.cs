using Micon.LotterySystem.Models;
using Micon.LotterySystem.Models.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Micon.LotterySystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController(RoleManager<ApplicationRole> roleManager) : ControllerBase
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
            var role = await roleManager.FindByNameAsync(roleName);
            if (role == null)
            {
                return NotFound();
            }
            var result = await roleManager.DeleteAsync(role);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            return Ok();
        }

        [Authorize]
        [HttpGet(nameof(RoleList))]
        public async Task<IActionResult> RoleList()
        {
            var roles = await roleManager.Roles
                .Select(x=>new SendRole(x))
                .ToListAsync();
            return Ok(roles);
        }
    }
}
