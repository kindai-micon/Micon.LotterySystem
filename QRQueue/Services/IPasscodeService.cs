namespace QRQueue.Services
{
    public interface IPasscodeService
    {
        public Task<bool> CheckPascodeAsync(string pascode);
        public bool CheckPascode(string pascode);
        public string GetPasscode();
    }
}
