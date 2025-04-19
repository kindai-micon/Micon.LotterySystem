using Micon.LotterySystem.Models;
using Micon.LotterySystem.Models.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using LotteryGroup = Micon.LotterySystem.Models.LotteryGroup;

namespace Micon.LotterySystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LotteryGroupController(ApplicationDbContext applicationDbContext) : ControllerBase
    {
        [Authorize]
        [HttpGet(nameof(List))]
        public async Task<IActionResult> List()
        {
            var list = await applicationDbContext.LotteryGroups.Select(x => new { name = x.Name, id = x.DisplayId.ToString()}).ToListAsync();
            return Ok(list);
        }
        [Authorize(Policy = "LotteryGroupManagement")]
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

                await applicationDbContext.LotteryGroups.AddAsync(lotteryGroup);
                await applicationDbContext.SaveChangesAsync();
            }
            return Ok();
        }
        [Authorize(Policy = "LotteryGroupManagement")]
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
        [Authorize(Policy = "LotteryGroupManagement")]
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
        [Authorize()]

        [HttpPost(nameof(LoadTicketJson))]
        public async Task<IActionResult> LoadTicketJson([FromBody] idAndName idAndName)
        {
            var raw = System.IO.File.ReadAllText(idAndName.json);
            var tmp = JsonSerializer.Deserialize<jsonTicket[]>(raw);
            var group = await applicationDbContext.LotteryGroups.Where(x => x.DisplayId.ToString() == idAndName.groupId).FirstOrDefaultAsync();
            foreach(var item in tmp) {
                Ticket ticket = new Ticket();
                ticket.Status = TicketStatus.Invalid;
                ticket.Number = item.number;
                ticket.DisplayId = item.displayId;
                applicationDbContext.Tickets.Add(ticket);
                group.Tickets.Add(ticket);
            }
            await applicationDbContext.SaveChangesAsync();
            return Ok();
        }
        
    }
    public record idAndName (string groupId,string json);
    public record jsonTicket(long number,Guid displayId);
}

