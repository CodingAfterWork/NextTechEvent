var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.NextTechEvent>("nexttechevent");

builder.Build().Run();
