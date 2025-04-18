using Micon.LotterySystem.Models;
using Micon.LotterySystem.Models.API;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LotteryGroup = Micon.LotterySystem.Models.LotteryGroup;

namespace Micon.LotterySystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LotteryGroupController(ApplicationDbContext applicationDbContext) : ControllerBase
    {
        [HttpGet(nameof(List))]
        public async Task<IActionResult> List()
        {
            var list = await applicationDbContext.LotteryGroups.Select(x => new { name = x.Name, id = x.DisplayId.ToString()}).ToListAsync();
            return Ok(list);
        }
        [HttpPost(nameof(Create))]
        public async Task<IActionResult> Create([FromBody] string name)
        {
            if (applicationDbContext.LotteryGroups.Any(x => x.Name == name))
            {
                return BadRequest("Lottery group already exists");
            }
            else
            {
                var lotteryGroup = new LotteryGroup()
                {
                    Name = name,
                    TicketInfo = new TicketInfo()
                };
#
                await applicationDbContext.LotteryGroups.AddAsync(lotteryGroup);
                await applicationDbContext.SaveChangesAsync();
            }
            return Ok();
        }

        [HttpPost(nameof(Delete))]
        public async Task<IActionResult> Delete([FromBody] string name)
        {
            var lotteryGroup = await applicationDbContext.LotteryGroups.FirstOrDefaultAsync(x => x.Name == name);
            if (lotteryGroup == null)
            {
                return NotFound();
            }
            applicationDbContext.LotteryGroups.Remove(lotteryGroup);
            await applicationDbContext.SaveChangesAsync();
            return Ok();
        }
        [HttpPut(nameof(Rename))]
        public async Task<IActionResult> Rename([FromBody] RenameModel renameModel)
        {
            var lotteryGroup = await applicationDbContext.LotteryGroups.FirstOrDefaultAsync(x => x.Name == renameModel.Name);
            if (lotteryGroup == null)
            {
                return NotFound();
            }
            lotteryGroup.Name = renameModel.NewName;
            applicationDbContext.LotteryGroups.Update(lotteryGroup);
            await applicationDbContext.SaveChangesAsync();
            return Ok();
        }
        [HttpGet(nameof(Name))]
        public async Task<IActionResult> Name([FromQuery] string id)
        {
            var group = await applicationDbContext.LotteryGroups.FirstOrDefaultAsync(x => x.DisplayId.ToString() == id);
            if(group == null)
            {
                return NotFound();
            }
            return Ok(group?.Name);
        }
    }
}
