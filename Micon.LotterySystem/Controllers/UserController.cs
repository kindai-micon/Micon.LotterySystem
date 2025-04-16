
using Micon.LotterySystem.Models;
using Micon.LotterySystem.Models.API;
using Micon.LotterySystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Micon.LotterySystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(
        UserManager<ApplicationUser> userManager, 
        SignInManager<ApplicationUser> signInManager, 
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext applicationDbContext,
        IAuthorityScanService authorityScanService,
        IPasscodeService passcodeService) : ControllerBase
    {
        [HttpGet(nameof(MyInfo))]
        public async Task<IActionResult> MyInfo()
        {
            
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            SendUser sendUser = new SendUser(user);
            var roleStrList = await userManager.GetRolesAsync(user);
            sendUser.Roles =
                await roleManager.Roles.Include(x => x.Authorities).Where(x => roleStrList.Contains(x.Name))
                    .Select(r => new SendRole(r))
                    .ToListAsync();
            return Ok(sendUser);
        }

        [Authorize("UserManagement")]
        [HttpGet(nameof(UserInfo))]
        public async Task<IActionResult> UserInfo([FromQuery]string userName)
        {

            var user = await userManager.FindByNameAsync(userName);
            if (user == null)
            {
                return NotFound();
            }
            SendUser sendUser = new SendUser(user);
            var roleStrList = await userManager.GetRolesAsync(user);
            sendUser.Roles =
                await roleManager.Roles.Include(x => x.Authorities).Where(x => roleStrList.Contains(x.Name))
                    .Select(r => new SendRole(r))
                    .ToListAsync();
            return Ok(sendUser);
        }

        [Authorize("UserManagement")]
        [HttpPost(nameof(DeleteUser))]
        public async Task<IActionResult> DeleteUser([FromBody] string userName)
        {
            var my = await userManager.GetUserAsync(User);
            var user = await userManager.FindByNameAsync(userName);

            var result = await userManager.DeleteAsync(my);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            if(my.Id == user.Id)
            {
                await signInManager.SignOutAsync();
            }
            return Ok();
        }

        [HttpPost(nameof(LoginByEmail))]
        public async Task<IActionResult> LoginByEmail([FromBody] LoginEmailModel loginModel)
        {
            var user = await userManager.FindByEmailAsync(loginModel.Email);
            if (user == null)
            {
                return NotFound();
            }
            var result = await signInManager.PasswordSignInAsync(user, loginModel.Password, true, false);
            if (!result.Succeeded)
            {
                return BadRequest();
            }
            return Ok();
        }

        [HttpPost(nameof(LoginByUserName))]
        public async Task<IActionResult> LoginByUserName([FromBody] LoginNameModel loginModel)
        {
            var user = await userManager.FindByNameAsync(loginModel.UserName);
            if (user == null)
            {
                return NotFound();
            }
            var result = await signInManager.PasswordSignInAsync(user, loginModel.Password, true, false);
            if (!result.Succeeded)
            {
                return BadRequest();
            }
            var sendUser = new SendUser(user);
            var roleStrList = await userManager.GetRolesAsync(user);
            sendUser.Roles =
                await roleManager.Roles.Include(x => x.Authorities).Where(x => roleStrList.Contains(x.Name))
                .Select(r => new SendRole(r))
                .ToListAsync();
            return Ok(sendUser);
        }
        
        [Authorize(Policy = "UserManagement")]
        [HttpPost(nameof(Register))]
        public async Task<IActionResult> Register([FromBody] RegisterModel registerModel)
        {

            var user = await userManager.FindByNameAsync(registerModel.UserName);
            if (user == null)
            {
                ApplicationUser applicationUser = new ApplicationUser(registerModel.UserName);
                var result = await userManager.CreateAsync(applicationUser, registerModel.Password);
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }
                if(registerModel.Email != null)
                {
                    result = await userManager.SetEmailAsync(applicationUser, registerModel.Email);

                }
                if (!result.Succeeded)
                {
                    return BadRequest(result.Errors);
                }
            }
            else
            {
                BadRequest(new IdentityError[] { new IdentityError() { Code = "Exists", Description = "存在するユーザー名です" } });
            }
            return Ok();
        }
        [HttpPost(nameof(InitialRegister))]
        public async Task<IActionResult> InitialRegister(InitialUser initialUser)
        {
            if (await passcodeService.CheckPascodeAsync(initialUser.Passcode))
            {
                if(initialUser.Password != initialUser.ConfirmPassword)
                {
                    return BadRequest(new IdentityError[] { new IdentityError() { Code = "Passcode", Description = "Passcodeが異なります" } });

                }
                ApplicationUser applicationUser = new ApplicationUser();
                applicationUser.UserName = initialUser.UserName;
                applicationUser.Email = initialUser.Email;
                var result = await userManager.CreateAsync(applicationUser, initialUser.Password);
                if(result.Succeeded == false)
                {
                    return BadRequest(result.Errors.ToArray());
                }
                ApplicationRole applicationRole = new ApplicationRole("Admin");
                result = await roleManager.CreateAsync(applicationRole );
                if(result.Succeeded == false)
                {
                    return BadRequest(result.Errors.ToArray());
                }
                List<Authority> authorities = new List<Authority>();
                foreach (var authority in authorityScanService.Authority)
                {
                    var authority1 = applicationDbContext.Authorities.Add(new Authority() { Name = authority});
                    authorities.Add(authority1.Entity);

                }
                applicationRole.Authorities.AddRange(authorities);

                result = await userManager.AddToRoleAsync(applicationUser,applicationRole.Name);
                if(result.Succeeded == false)
                {
                    return BadRequest(result.Errors.ToArray());
                }
                return Ok();
            }
            else
            {
                return BadRequest(new IdentityError[] { new IdentityError() { Code = "Passcode", Description = "Passcodeが異なります" } });
            }
        }
        [HttpGet(nameof(HasUser))]
        public async Task<IActionResult> HasUser()
        {
            int count = await userManager.Users.CountAsync();
            return Ok(count != 0);
        }
        [Authorize]
        [HttpPost(nameof(Logout))]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return Ok();
        }

        [Authorize("UserManagement")]
        [HttpGet(nameof(UserList))]
        public async Task<IActionResult> UserList()
        {
            var users = (await userManager.Users.ToListAsync());
            List<SendUser> sendUsers = new List<SendUser>();
            foreach (var user in users)
            {
                var sendUser = new SendUser(user);
                sendUsers.Add(sendUser);
                var roleStrList = await userManager.GetRolesAsync(user);
                sendUser.Roles =
                    await roleManager.Roles.Include(x => x.Authorities).Where(x => roleStrList.Contains(x.Name))
                    .Select(r => new SendRole(r))
                    .ToListAsync();
            }
            return Ok(sendUsers);
        }
        [Authorize(Policy = "UserRoleManagement")]
        [HttpPut(nameof(AddRole))]
        public async Task<IActionResult> AddRole([FromBody] UserRoleModel userRoleModel)
        {
            var user = await userManager.FindByNameAsync(userRoleModel.UserName);
            if (user == null)
            {
                return NotFound();
            }
            var role = await roleManager.FindByNameAsync(userRoleModel.RoleName);
            if (role == null)
            {
                return NotFound();
            }
            var result = await userManager.AddToRoleAsync(user, role.Name);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            return Ok();
        }

        [Authorize(Policy = "UserRoleManagement")]
        [HttpPut(nameof(RemoveRole))]
        public async Task<IActionResult> RemoveRole([FromBody] UserRoleModel userRoleModel)
        {
            var users = await userManager.GetUsersInRoleAsync(userRoleModel.RoleName);
            if(userRoleModel.RoleName == "Admin"&&users.Count <= 1)
            {
                return Conflict();
            }
            var user = await userManager.FindByNameAsync(userRoleModel.UserName);
            if (user == null)
            {
                return NotFound();
            }
            var role = await roleManager.FindByNameAsync(userRoleModel.RoleName);
            if (role == null)
            {
                return NotFound();
            }
            var result = await userManager.RemoveFromRoleAsync(user, role.Name);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }
            return Ok();

        }
    }
}
