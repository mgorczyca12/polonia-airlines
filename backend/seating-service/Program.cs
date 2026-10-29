using seating_service.Api.Extensions;
using seating_service.Application;
using seating_service.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddApi()
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

app.MapHealthChecks("/health");
app.Run();

public partial class Program;
