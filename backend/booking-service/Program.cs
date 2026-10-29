using booking_service.Api.Extensions;
using booking_service.Application;
using booking_service.Infrastructure;
using booking_service.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var schemaOnly = args.Contains("--schema-script", StringComparer.Ordinal);
var builder = WebApplication.CreateBuilder(args.Where(argument => argument != "--schema-script").ToArray());
builder.AddApplication().AddInfrastructure().AddApi();

var app = builder.Build();
if (schemaOnly)
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
    Console.WriteLine(database.Database.GenerateCreateScript());
    return;
}

await app.InitializeBooking();
app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();

public partial class Program;
