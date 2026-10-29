using booking_service.Api.Extensions;
using booking_service.Application;
using booking_service.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddApi()
    .AddApplication()
    .AddInfrastructure();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;
