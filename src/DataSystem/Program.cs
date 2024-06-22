using System.Reflection;
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
builder.Services.AddGrpc().AddJsonTranscoding();

// add services for swagger documentation generation
builder.Services.AddMvc();
builder.Services.AddGrpcSwagger();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API-Documentation", Version = "v1" });
    
    // enable proto code comments
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
    c.IncludeGrpcXmlComments(xmlFilename, includeControllerXmlComments: true);
});

var app = builder.Build();

// swagger
app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("v1/swagger.json", "API V1"); });

// map grpc service
app.MapGrpcService<DataService>();

app.Run();