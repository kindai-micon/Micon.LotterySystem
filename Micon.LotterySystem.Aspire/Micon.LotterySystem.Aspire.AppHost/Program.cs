using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var db = builder.AddPostgres("LotteryDb")
    .AddDatabase("lottery-db");
var main = builder.AddProject<Micon_LotterySystem>("main")
    .WithReference(db)
    .WaitFor(db);

builder.AddExecutable("desktop", "dotnet", "../../Micon.LotterySystem.Desktop", "run", "--launch-profile", "Development")
    .WithEnvironment("BACKEND_URL", main.GetEndpoint("https"))
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WaitFor(main);

builder.Build().Run();
