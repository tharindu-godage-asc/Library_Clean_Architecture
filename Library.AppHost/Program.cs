var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume();

var libraryDb = postgres.AddDatabase("LibraryDb");

builder.AddProject<Projects.Library_Api>("library-api")
    .WithReference(libraryDb)
    .WaitFor(libraryDb);

builder.Build().Run();
