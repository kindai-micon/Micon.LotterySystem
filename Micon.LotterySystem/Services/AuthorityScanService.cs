using Microsoft.AspNetCore.Authorization;
using System.Reflection;

namespace Micon.LotterySystem.Services
{
    public class AuthorityScanService:IAuthorityScanService
    {
        public HashSet<string> Authority { get; private set; } = new HashSet<string>();
        Assembly assembly = Assembly.GetExecutingAssembly();
        public AuthorityScanService()
        {

            Scan();
        }
        public void Scan()
        {
            var types = assembly.GetTypes();
            foreach(var type in types)
            {
                var auth = type.GetCustomAttribute<AuthorizeAttribute>();
                if(auth != null)
                {
                    if(auth.Policy != null)
                    {
                        Authority.Add(auth.Policy);
                    }
                }
                var methods = type.GetMethods();
                foreach(var method in methods)
                {
                    var authMethod = method.GetCustomAttribute<AuthorizeAttribute>();
                    if(authMethod != null)
                    {
                        if(authMethod.Policy != null)
                        {
                            Authority.Add(authMethod.Policy);
                        }
                    }
                }
            }
            

        }
    }
}
