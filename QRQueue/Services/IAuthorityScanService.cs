using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace QRQueue.Services
{
    public interface IAuthorityScanService
    {
        public  HashSet<string> Authority { get; }
        public void Scan();
    }
}
