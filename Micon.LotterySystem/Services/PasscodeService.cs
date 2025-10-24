using Micon.LotterySystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Micon.LotterySystem.Services
{
    public class PasscodeService : IPasscodeService
    {
        private readonly UserManager<ApplicationUser> userManager;
        private static string Passcode = null;

        public PasscodeService(UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;

            if (Passcode != null)
            {
                return;
            }

            // 初回起動時にパスコードを生成
            Passcode = GenerateNewPasscode();
            Console.WriteLine("passcode:" + Passcode);
        }

        /// <summary>
        /// 新しいパスコードを生成する
        /// </summary>
        private string GenerateNewPasscode()
        {
            Random random = new Random();
            string newPasscode = "";
            for (int i = 0; i < 10; i++)
            {
                newPasscode += random.Next(0, 10).ToString();
            }
            return newPasscode;
        }

        public async Task<bool> CheckPascodeAsync(string passcode)
        {
            Console.WriteLine("passcode:" + Passcode);

            var usersCount = await userManager.Users.CountAsync();
            if (usersCount != 0)
            {
                return false;
            }

            return passcode == Passcode;
        }

        public bool CheckPascode(string passcode)
        {
            Console.WriteLine("passcode:" + Passcode);

            var usersCount = userManager.Users.Count();
            if (usersCount != 0)
            {
                return false;
            }

            return passcode == Passcode;
        }

        /// <summary>
        /// 現在のパスコードを取得する
        /// </summary>
        public string GetPasscode()
        {
            return Passcode;
        }
    }
}
