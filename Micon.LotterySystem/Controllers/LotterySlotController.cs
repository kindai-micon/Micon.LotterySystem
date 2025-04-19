using Micon.LotterySystem.Models.API;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Micon.LotterySystem;
using Microsoft.AspNetCore.Authorization;

namespace Micon.LotterySystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LotterySlotController(ApplicationDbContext applicationDbContext) : ControllerBase
    {
        [Authorize]

        [HttpGet(@$"{nameof(List)}/{{id}}")]
        public async Task<IActionResult> List(string id)
        {
            var group = await applicationDbContext.LotteryGroups.FirstOrDefaultAsync(x => x.DisplayId.ToString() == id);
            if(group == null)
            {
                return NotFound();
            }
            var list = await applicationDbContext.LotterySlots.Where(x => x.LotteryGroupId == group.Id)
                .OrderBy(x => x.Order)
                .Select<Models.LotterySlots, LotterySlots>(x=>
                new LotterySlots 
                { 
                    DeadLine = x.DeadLine==DateTimeOffset.MaxValue?null:x.DeadLine,
                    Name =x.Name ,
                    LotteryId=id,
                    SlotId = x.DisplayId.ToString(),
                    Merchandise = x.Merchandise,
                    NumberOfFrames = x.NumberOfFrames
                })
                .ToListAsync();
            return Ok(list);
        }
        [Authorize(Policy = "LotterySlotManagement")]

        [HttpPost(nameof(Create))]
        public async Task<IActionResult> Create(LotterySlots lotterySlots)
        {
            var group = await applicationDbContext.LotteryGroups.FirstOrDefaultAsync(x => x.DisplayId.ToString() == lotterySlots.LotteryId);

            var slots = await applicationDbContext.LotterySlots.Where(x => x.LotteryGroupId == group.Id)
                .OrderBy(x => x.Order)
                .ToListAsync();
            var newslot = new Models.LotterySlots() { Order = 0, LotteryGroupId = group.Id, Merchandise = lotterySlots.Merchandise, Name = lotterySlots.Name, DeadLine = (lotterySlots.DeadLine ?? DateTimeOffset.MaxValue).ToOffset(new TimeSpan(0)) ,NumberOfFrames =lotterySlots.NumberOfFrames };
            applicationDbContext.Add(newslot);
            slots.Insert(0, newslot);
            slots = slots.Select((x, i) => { x.Order = i; return x; }).ToList();
            for (int i = 1; i < slots.Count; i++)
            {
                applicationDbContext.Update(slots[i]);
            }
            await applicationDbContext.SaveChangesAsync();
            return Ok();
        }
        [Authorize(Policy = "LotterySlotManagement")]

        [HttpPut(nameof(Update))]
        public async Task<IActionResult> Update(LotterySlots lotterySlots)
        {
            var group = await applicationDbContext.LotteryGroups.FirstOrDefaultAsync(x => x.DisplayId.ToString() == lotterySlots.LotteryId);
            var slots = await applicationDbContext.LotterySlots.Where(x => x.LotteryGroupId == group.Id)
                .OrderBy(x => x.Order)
                .ToListAsync();

            var target = slots.FirstOrDefault(x => x.DisplayId.ToString() == lotterySlots.SlotId);
            target.Name = lotterySlots.Name;
            target.Merchandise = lotterySlots.Merchandise;
            target.DeadLine = (lotterySlots.DeadLine ?? DateTimeOffset.MaxValue).ToOffset(new TimeSpan(0));
            target.NumberOfFrames = lotterySlots.NumberOfFrames;
            applicationDbContext.Update(target);
            await applicationDbContext.SaveChangesAsync();
            return Ok();
        }
        [Authorize(Policy = "LotterySlotManagement")]

        [HttpPost(nameof(Delete))]
        public async Task<IActionResult> Delete([FromBody] string id)
        {
            var group = await applicationDbContext.LotterySlots.Where(x => x.DisplayId.ToString() == id)
                .Include(x=>x.LotteryGroup)
                .Select(x => x.LotteryGroup)
                .FirstOrDefaultAsync();
            var slots = await applicationDbContext.LotterySlots.Where(x => x.LotteryGroupId == group.Id)
                .OrderBy(x => x.Order)
                .ToListAsync();

            var target = slots.FirstOrDefault(x => x.DisplayId.ToString() == id);
            if (target == null)
            {
                return NotFound();
            }
            slots.Remove(target);
            slots = slots.Select((x, i) => { x.Order = i; return x; }).ToList();

            applicationDbContext.Remove(target);
            await applicationDbContext.SaveChangesAsync();
            return Ok();
        }
        [Authorize(Policy = "LotterySlotManagement")]

        [HttpPut(nameof(MoveIndex))]
        public async Task<IActionResult> MoveIndex(MoveIndex moveIndex)
        {
            var groupId = await applicationDbContext.LotterySlots
                .Where(x => x.DisplayId.ToString() == moveIndex.Id)
                .Select(x => x.LotteryGroupId)
                .FirstOrDefaultAsync();

            var slots = await applicationDbContext.LotterySlots
                .Where(x => x.LotteryGroupId == groupId)
                .OrderBy(x => x.Order)
                .ToListAsync();

            var target = slots.FirstOrDefault(x => x.DisplayId.ToString() == moveIndex.Id);
            slots.Remove(target);
            slots.Insert(moveIndex.newIndex, target);
            slots = slots.Select((x, i) => { x.Order = i; return x; }).ToList();

            for (int i = 1; i < slots.Count; i++)
            {
                applicationDbContext.Update(slots[i]);
            }
            applicationDbContext.Update(target);
            await applicationDbContext.SaveChangesAsync();
            return Ok();
        }
    }
}
