using Microsoft.EntityFrameworkCore;
using MyFinances.Data;
using MyFinances.Services;
using MySqlConnector;

var builder = WebApplication.CreateBuilder(args);

// Add database context to the services
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new ArgumentException(nameof(connectionString));
}

builder.Services.AddDbContext<AppDBContext>(options => options.UseMySQL(connectionString));
builder.Services.AddMySqlDataSource(connectionString);
builder.Services.AddScoped<IMyCsvService, MyCsvService>();
builder.Services.AddScoped<IDataTableJsonService, DataTableJsonService>();
builder.Services.AddScoped<IComboFillService, ComboFillService>();
builder.Services.AddScoped<IMyDetailsService, MyDetailsService>();

// Add services to the container.
builder.Services.AddRazorPages();

//add session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

//Add memory cache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Enable Session BEFORE MapRazorPages
app.UseSession();

app.MapRazorPages();

app.Run();
