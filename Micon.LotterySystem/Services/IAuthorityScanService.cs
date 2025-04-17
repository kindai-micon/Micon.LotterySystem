using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace Micon.LotterySystem.Services
{
    public interface IAuthorityScanService
    {
        public  HashSet<string> Authority { get; }
        public void Scan();
    }
}
