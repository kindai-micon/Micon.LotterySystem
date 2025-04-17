namespace Micon.LotterySystem.Services
{
    public interface IPasscodeService
    {
        public Task<bool> CheckPascodeAsync(string pascode);
        public bool CheckPascode(string pascode);

    }
}
