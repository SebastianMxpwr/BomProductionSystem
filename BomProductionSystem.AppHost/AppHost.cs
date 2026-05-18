var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.BomProduction_QueryService>("bomproduction-queryservice");

builder.Build().Run();
