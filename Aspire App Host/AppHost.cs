var builder = DistributedApplication.CreateBuilder(args);

// AddConnectionString("LibraryDb") kept re-triggering the dashboard's interactive
// "waiting for user input" prompt on every run instead of picking up the value already
// saved to user secrets (ConnectionStrings:LibraryDb) — root cause not pinned down, but
// setting the env var directly bypasses that resolution path entirely and points at the
// local postgres-db container from docker-compose.yml (matches .env: DB_NAME=librarydb2,
// DB_USER=postgres, DB_PASSWORD=postgres).
builder.AddProject<Projects.Library_Api>("api")
    .WithEnvironment("ConnectionStrings__LibraryDb", "Host=localhost;Port=5432;Database=librarydb2;Username=postgres;Password=postgres")
    .WithExternalHttpEndpoints()
    .WithHttpsEndpoint(port: 7282, name: "https")
    .WithHttpEndpoint(port: 5281, name: "http");

builder.Build().Run();
