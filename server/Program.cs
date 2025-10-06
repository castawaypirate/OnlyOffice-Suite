using Microsoft.EntityFrameworkCore;
using OnlyOfficeServer.Data;
using OnlyOfficeServer.Services;
using OnlyOfficeServer.Repositories;
using OnlyOfficeServer.Managers;
using OnlyOfficeServer.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=onlyoffice.db"));

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services
builder.Services.AddScoped<FileService>();

// Add repositories and managers
builder.Services.AddScoped<IInstallationRepository, InstallationRepository>();
builder.Services.AddScoped<InstallationManager>();

// Add controllers
builder.Services.AddControllers();

// Add SignalR
builder.Services.AddSignalR();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add CORS for Angular frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Apply migrations and seed database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.Migrate();
    DatabaseSeederService.SeedDatabase(context);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAngular");
app.UseSession();
app.UseHttpsRedirection();

app.MapControllers();
app.MapHub<OnlyOfficeHub>("/hubs/onlyoffice");

app.Run();
