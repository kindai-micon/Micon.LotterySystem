using Projects;

var builder = DistributedApplication.CreateBuilder(args);
var db = builder.AddPostgres("LotteryDb")
    .AddDatabase("lottery-db");
builder.AddProject<Micon_LotterySystem>("main")
    .WithReference(db)
    .WaitFor(db);
builder.Build().Run();
