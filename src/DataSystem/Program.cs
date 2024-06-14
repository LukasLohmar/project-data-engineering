using DataSystem.Database;
using DataSystem.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// add grpc service and database service to application
builder.Services.AddDbContextFactory<ApplicationContext>(options =>
{
    options.UseNpgsql(Environment.GetEnvironmentVariable("SQL_SERVER"));
});
builder.Services.AddGrpc();
builder.Services.AddControllers();

var app = builder.Build();

// map grpc service
app.MapGrpcService<DataService>();

// map get requests
app.MapControllers();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

app.Run();