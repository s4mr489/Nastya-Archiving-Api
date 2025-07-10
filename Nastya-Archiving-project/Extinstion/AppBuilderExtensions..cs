using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Helper;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Nastya_Archiving_project.Extinstion
{
    public static class AppBuilderExtensions
    {


        public static IApplicationBuilder UseSwaggerAuthorized(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SwaggerBasicAuthMiddleware>();
        }

        public static IApplicationBuilder UseAuth(this IApplicationBuilder app)
        {
            app.UseAuthentication();
            app.UseAuthorization();

            return app;
        }
        public static IApplicationBuilder UseCustomSwagger(this IApplicationBuilder app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(opt =>
            {
                opt.SwaggerEndpoint("/swagger/Main/swagger.json", "v1");
                opt.RoutePrefix = string.Empty;

                opt.InjectStylesheet("/swagger/swagger-dark.css");
                opt.InjectJavascript("/swagger/theme-switcher.js");

                opt.DocumentTitle = "VotingSystem";
                opt.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
                opt.DisplayRequestDuration();

                opt.EnableFilter();
                opt.EnableValidator();
                opt.EnableDeepLinking();
                opt.EnablePersistAuthorization();
                opt.EnableTryItOutByDefault();

            });

            return app;
        }

        public static IApplicationBuilder UseCustomSwaggerWithAuth(this IApplicationBuilder app, string Title)
        {
            string Title2 = Title;
            app.UseSwagger();
            app.UseSwaggerAuthorized();

            app.UseSwaggerUI(delegate (SwaggerUIOptions opt)
            {
                opt.SwaggerEndpoint("/swagger/Main/swagger.json", "v1");
                opt.InjectStylesheet("/swagger/swagger-dark.css");
                opt.InjectJavascript("/swagger/theme-switcher.js");
                opt.DocumentTitle = Title2;
                opt.DocExpansion(DocExpansion.None);
                opt.DisplayRequestDuration();
                opt.EnableFilter();
                opt.EnableValidator();
                opt.EnableDeepLinking();
                opt.EnablePersistAuthorization();
                opt.EnableTryItOutByDefault();
            });
            return app;
        }

        public static async Task<IApplicationBuilder> UseSeeder(this IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var dataContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();  // Make sure AppData is your context type
                var seeder = new Seeder(dataContext);

                await seeder.SeedSuperAdmin("SuperAdmin", "SuperAdmin"); // You can add a password for the SuperAdmin
            }

            return app;
        }
    }
}
