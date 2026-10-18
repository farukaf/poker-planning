using Blazored.LocalStorage;
using Blazored.Toast;
using PokerPlanning.BackgroundServices;
using PokerPlanning.Configuration;
using PokerPlanning.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogConfig();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddBlazoredToast();

builder.Services.AddSingleton<IRoomService, RoomService>();
builder.Services.AddScoped<IPlayerService, PlayerService>();
builder.Services.AddHostedService<JanitorService>();

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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();

public partial class Program { }
