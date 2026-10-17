using YMS.Api;
using YMS.Application;
using YMS.Infrastructure;
using YMS.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApi(builder.Environment);

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
    await app.Services.MigrateDatabaseAsync();

if (app.Environment.IsDevelopment())
    await app.Services.SeedDevelopmentOwnerAsync(app.Configuration);

app.UseApi();

app.Run();

public partial class Program;
