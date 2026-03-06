using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var db = builder.AddPostgres("LotteryDb")
    .AddDatabase("lottery-db");
var main = builder.AddProject<Micon_LotterySystem>("main")
    .WithReference(db)
    .WaitFor(db);

// 環境変数 LAUNCH_DESKTOP=true のときだけDesktopアプリを起動
var launchDesktop = builder.Configuration["LAUNCH_DESKTOP"]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true
                    || args.Contains("--desktop");

if (launchDesktop)
{
    builder.AddExecutable("desktop", "dotnet", "../../Micon.LotterySystem.Desktop", "run", "--launch-profile", "Development")
        .WithEnvironment("BACKEND_URL", main.GetEndpoint("https"))
        .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
        .WaitFor(main);
}

builder.Build().Run();
