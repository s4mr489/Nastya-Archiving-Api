using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Extinstion;
using Nastya_Archiving_project.Helper;
using Nastya_Archiving_project.Seginal;

var builder = WebApplication.CreateBuilder(args);
ConfigProvider.config = builder.Configuration;

builder.Services.AddControllers();
builder.Services.AddOpenApi();

//Create the connection with the Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//add Policy for CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        builder => builder.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader());
});


builder.Services.AddAuthConfig();
builder.Services.AddSwaggerConfig();
builder.Services.AddServices();
builder.Services.AddMapperConfig();

var app = builder.Build();
app.UseCors("AllowAllOrigins");


app.UseHsts();
app.MapOpenApi();

//app.UseRateLimiter();
app.UseAuth();
app.UseSeeder();

app.UseCustomSwaggerWithAuth("Nastya-Archiving-Swagger");

app.UseHttpsRedirection();
app.UseStaticFiles();


app.MapHub<MailNotificationHub>("/mailNotificationHub");
app.MapGet("/", context =>
{
    context.Response.Redirect("/swagger");
    return Task.CompletedTask;
});
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
