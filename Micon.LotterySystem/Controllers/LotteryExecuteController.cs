using Micon.LotterySystem.Hubs;
using Micon.LotterySystem.Models;
using Micon.LotterySystem.Models.API;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Micon.LotterySystem.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LotteryExecuteController(IHubContext<LotteryHub> lotteryHubContext,ApplicationDbContext applicationDbContext) : ControllerBase
    {

        [HttpGet(nameof(ExecutingSlotState))]
        public async Task<IActionResult> ExecutingSlotState([FromQuery] string groupId)
        {
            var group = await applicationDbContext.LotteryGroups.Where(x => x.DisplayId.ToString() == groupId).FirstOrDefaultAsync();
            var slot = await applicationDbContext.LotterySlots
                .Where(x => x.LotteryGroupId == group.Id && !(x.Status == Models.SlotStatus.TargetLottery || x.Status == SlotStatus.ViewResult || x.Status == SlotStatus.DuringAnimation))
                .Include(x => x.Tickets)
                .Select(
                x => new WinningModel()
                {
                    SlotId = x.Id.ToString(),
                    Name = x.Name,
                    NumberOfFrames = x.NumberOfFrames,
                    Status = x.Status,
                    Tickets = x.Tickets.Select(x => new WinnerTicket()
                    {
                        Number = x.Number.ToString(),
                        Status = x.Status
                    }).ToList()
                })
                .FirstOrDefaultAsync();
            return Ok(slot);
        }
        [HttpPut(nameof(TargetSlot))]

        public async Task<IActionResult> TargetSlot([FromBody] string slotId)
        {

            var group = await applicationDbContext.LotterySlots.Where(x => x.DisplayId.ToString() == slotId).Include(x => x.LotteryGroup).Select(x => x.LotteryGroup).FirstOrDefaultAsync();


            if (group == null)
            {
                return NotFound();
            }

            if(applicationDbContext.LotterySlots.Where(x=>x.LotteryGroupId==group.Id&& !(x.Status==SlotStatus.ViewResult || x.Status == SlotStatus.BeforeTheLottery || x.Status == SlotStatus.StopExchange || x.Status == SlotStatus.Exchange)).Any())
            {
                return Conflict();
            }

            var viewedList = applicationDbContext.LotterySlots.Where(x => x.Status == SlotStatus.ViewResult).ToList();
            foreach(var viewResult in viewedList)
            {
                viewResult.Status = SlotStatus.Exchange;
                applicationDbContext.Update(viewResult);
                await applicationDbContext.SaveChangesAsync();
            }
            var slot = await applicationDbContext.LotterySlots.Where(x => x.DisplayId.ToString() == slotId).FirstOrDefaultAsync();
            if (slot == null)
            {
                return NotFound();
            }
            if (!(slot.Status == SlotStatus.StopExchange || slot.Status == SlotStatus.BeforeTheLottery))
            {
                return Conflict();
            }
            slot.Status = SlotStatus.DuringAnimation;
            applicationDbContext.Update(slot);
            await applicationDbContext.SaveChangesAsync();
            await lotteryHubContext.Clients.Group(group.DisplayId.ToString()).SendAsync("SetTarget", slot.DisplayId);

            return Ok();
        }
        [HttpPut(nameof(AnimationExecute))]

        public async Task<IActionResult> AnimationExecute([FromBody]string slotId)
        {
            var group = await applicationDbContext.LotterySlots.Where(x => x.DisplayId.ToString() == slotId).Include(x=>x.LotteryGroup).Select(x=>x.LotteryGroup).FirstOrDefaultAsync();
            
            if(group == null)
            {
                return NotFound();
            }

            if (applicationDbContext.LotterySlots.Where(x => x.LotteryGroupId == group.Id && !(x.Status == SlotStatus.BeforeTheLottery || x.Status == SlotStatus.StopExchange || x.Status == SlotStatus.Exchange)).Count() != 1)
            {
                return Conflict();
            }
            var slot = await applicationDbContext.LotterySlots.Where(x=>x.DisplayId.ToString() == slotId).FirstOrDefaultAsync();
            if (slot == null)
            {
                return NotFound();
            }

            if (slot.Status != SlotStatus.TargetLottery)
            {
                return Conflict();
            }
            slot.Status = SlotStatus.DuringAnimation;
            applicationDbContext.Update(slot);
            await applicationDbContext.SaveChangesAsync();
            await lotteryHubContext.Clients.Group(group.DisplayId.ToString()).SendAsync("AnimationStart", slot.DisplayId);
            return Ok();
        }
        [HttpPut(nameof(LotteryExecute))]

        public async Task<IActionResult> LotteryExecute([FromBody] string slotId)
        {
            var group = await applicationDbContext.LotterySlots
                .Where(x => x.DisplayId.ToString() == slotId)
                .Include(x => x.LotteryGroup)
                .Select(x => x.LotteryGroup)
                .FirstOrDefaultAsync();
            if(group == null)
            {
                return NotFound();  
            }
            if (applicationDbContext.LotterySlots.Where(x => x.LotteryGroupId == group.Id && !(x.Status == SlotStatus.BeforeTheLottery || x.Status == SlotStatus.StopExchange)).Count() != 1)
            {
                return Conflict();
            }
            var slot = await applicationDbContext.LotterySlots
                .Where(x => x.DisplayId.ToString() == slotId)
                .Include(x=>x.Tickets)
                .FirstOrDefaultAsync();
            if(slot == null)
            {
                return NotFound();
            }
            if(slot.Status != SlotStatus.DuringAnimation)
            {
                return Conflict();
            }
            slot.Status = SlotStatus.ViewResult;
            var tickets = await applicationDbContext.Tickets
                .Where(x => x.LotteryGroupId == group.Id && x.Status == TicketStatus.Valid)
                .OrderBy(x => x.Id)
                .ToListAsync();
            if (tickets.Any())
            {
                return NotFound();
            }

            long count = slot.NumberOfFrames - slot.Tickets.Count;
            HashSet<Ticket> winner = new HashSet<Ticket>();
            while(winner.Count < count)
            {
                int index = Random.Shared.Next(0, tickets.Count);
                winner.Add(tickets[index]);
            }
            var winnerList = winner.OrderBy(x=>x.Id).ToList();
            foreach(var ticket in winnerList)
            {
                slot.Tickets.Add(ticket);
                ticket.Status = TicketStatus.Winner;
                applicationDbContext.Update(ticket);
            }
            
            await lotteryHubContext.Clients.Group(group.DisplayId.ToString()).SendAsync("SubmitLottery", slot.DisplayId);
            return Ok();
        }
        [HttpPut(nameof(ViewStop))]

        public async Task<IActionResult> ViewStop([FromBody] string slotId)
        {

            var group = await applicationDbContext.LotterySlots
                .Where(x => x.DisplayId.ToString() == slotId)
                .Include(x=>x.LotteryGroup)
                .Select(x=>x.LotteryGroup)
                .FirstOrDefaultAsync();
            if (group == null)
            {
                return NotFound();
            }
            if (applicationDbContext.LotterySlots.Where(x => x.LotteryGroupId == group.Id && !(x.Status == SlotStatus.BeforeTheLottery || x.Status == SlotStatus.StopExchange)).Count() != 1)
            {
                return Conflict();
            }

            var slot = await applicationDbContext.LotterySlots
                .Where(x => x.DisplayId.ToString() == slotId)
                .Include(x => x.Tickets)
                .FirstOrDefaultAsync();
            if (slot == null)
            {
                return NotFound();
            }
            if (slot.Status != SlotStatus.ViewResult)
            {
                return Conflict();
            }
            slot.Status = SlotStatus.Exchange;

            applicationDbContext.Update(slot);
            await applicationDbContext.SaveChangesAsync();
            await lotteryHubContext.Clients.Group(group.DisplayId.ToString()).SendAsync("ViewStop", slot.DisplayId);

            return Ok();
                
        }

        [HttpPut(nameof(ExchangeStop))]

        public async Task<IActionResult> ExchangeStop([FromBody] string slotId)
        {
            var group = await applicationDbContext.LotterySlots
                .Where(x => x.DisplayId.ToString() == slotId)
                .Include(x => x.LotteryGroup)
                .Select(x => x.LotteryGroup)
                .FirstOrDefaultAsync();
            if (group == null)
            {
                return NotFound();
            }
            
            var slot = await applicationDbContext.LotterySlots
                .Where(x => x.DisplayId.ToString() == slotId)
                .Include(x => x.Tickets)
                .FirstOrDefaultAsync();
            if (slot.Status != SlotStatus.Exchange&& slot.Status != SlotStatus.ViewResult)
            {
                return Conflict();
            }
            var removeList = slot.Tickets.Where(x => x.Status != TicketStatus.Exchanged);
            foreach(var ticket in removeList)
            {
                ticket.Status = TicketStatus.Invalid;
                slot.Tickets.Remove(ticket);
                ticket.LotterySlotsId = null;
                ticket.LotterySlots = null;
                applicationDbContext.Update(ticket);
            }

            applicationDbContext.Update(slot);
            await applicationDbContext.SaveChangesAsync();
            await lotteryHubContext.Clients.Group(group.DisplayId.ToString()).SendAsync("ExchangeStop", slot.DisplayId);

            return Ok();
        }
        [HttpGet(nameof(LotterySlotState))]

        public async Task<IActionResult> LotterySlotState([FromQuery] string slotId)
        {
            var slot = await applicationDbContext.LotterySlots.Where(x => x.DisplayId.ToString() == slotId).Include(x=>x.Tickets).FirstOrDefaultAsync();
            var slotResult = new WinningModel()
            {
                SlotId = slot.DisplayId.ToString(),
                Status = slot.Status,
                Name = slot.Name,
                NumberOfFrames = slot.NumberOfFrames
            };

            foreach(var ticket in slot.Tickets)
            {
                slotResult.Tickets.Add(new WinnerTicket
                {
                    Number = ticket.Number.ToString(),
                    Status = ticket.Status
                });
            }
            return Ok(slotResult);
        }
    }
}
