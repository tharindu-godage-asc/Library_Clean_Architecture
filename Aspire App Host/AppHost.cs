var builder = DistributedApplication.CreateBuilder(args);

var libraryDb = builder.AddConnectionString("LibraryDb");

builder.AddProject<Projects.Library_Api>("api")
    .WithReference(libraryDb)
    .WithExternalHttpEndpoints();

builder.Build().Run();
