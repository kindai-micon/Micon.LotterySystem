using Micon.LotterySystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Micon.LotterySystem.Services
{
    public class PasscodeService:IPasscodeService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private static string Passcode = null;
        public PasscodeService(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;
            
            
            Random random = new Random();
            if(Passcode != null)
            {
                return;
            }
            for(int i = 0; i < 10; i++)
            {
                Passcode += random.Next(0, 10).ToString();

            
            }
            Console.WriteLine("passcode:"+Passcode);

        }
        public async Task<bool> CheckPascodeAsync(string passcode)
        {
            var usersCount = await userManager.Users.CountAsync();
            if(usersCount != 0)
            {
                return false;
            }

            return passcode == Passcode;
        }
        public bool CheckPascode(string passcode)
        {
            var usersCount = userManager.Users.Count();
            if (usersCount != 0)
            {
                return false;
            }

            return passcode == Passcode;
        }
    }
}
