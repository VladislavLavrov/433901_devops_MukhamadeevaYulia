using Calculator.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Подключаем MVC
builder.Services.AddControllersWithViews();

// Получаем строку подключения из appsettings
string? mariadbConnectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

// Регистрируем контекст базы данных MariaDB
builder.Services.AddDbContext<CalculatorContext>(options =>
{
    options.UseMySql(
        mariadbConnectionString,
        new MySqlServerVersion(new Version(10, 5, 15))
    );
});

builder.Services.AddRazorPages();

var app = builder.Build();

// Настройка обработки ошибок
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Calculator/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Calculator}/{action=Index}/{id?}");

app.Run();