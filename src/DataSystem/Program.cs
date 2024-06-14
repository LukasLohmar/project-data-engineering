using DataSystem.Database;
using DataSystem.Service;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// add grpc service and database service to application
builder.Services.AddDbContextFactory<ApplicationContext>(options =>
{
    options.UseNpgsql(Environment.GetEnvironmentVariable("SQL_SERVER"));
});
builder.Services.AddGrpc();
builder.Services.AddControllers();

// add services for swagger documentation generation
builder.Services.AddMvc();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API-Documentation", Version = "v1" });
});

var app = builder.Build();

// map grpc service
app.MapGrpcService<DataService>();

// map get requests
app.MapControllers();
app.MapGet("/",
    () =>
        "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

// swagger
app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("v1/swagger.json", "API V1"); });

app.Run();