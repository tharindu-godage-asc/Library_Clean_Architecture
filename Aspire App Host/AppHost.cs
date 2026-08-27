var builder = DistributedApplication.CreateBuilder(args);

var libraryDb = builder.AddConnectionString("LibraryDb");

builder.AddProject<Projects.Library_Api>("api")
    .WithReference(libraryDb)
    .WithExternalHttpEndpoints()
    .WithHttpsEndpoint(port: 7282, name: "https")
    .WithHttpEndpoint(port: 5281, name: "http");

builder.Build().Run();
