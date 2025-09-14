using Nastya_Archiving_project.Data;
using Nastya_Archiving_project.Helper;
using Nastya_Archiving_project.Middleware;
using Nastya_Archiving_project.Services.encrpytion;
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

                opt.DocumentTitle = "ArchiveSystem";
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

        /// <summary>
        /// Configures static files middleware to enable direct file downloads to desktop
        /// </summary>
        /// <param name="app">The application builder</param>
        /// <returns>The application builder for chaining</returns>
        public static IApplicationBuilder UseDirectDownloads(this IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

            // Configure static files middleware with download options
            app.UseStaticFiles(new StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    string path = ctx.Context.Request.Path.Value;

                    // Check if this is a request for a file in the Downloads folder
                    if (path?.StartsWith("/Downloads/", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        // Get filename from the path
                        string filename = Path.GetFileName(path);

                        // Check if download parameter is present
                        bool isDownload = ctx.Context.Request.Query.ContainsKey("download");

                        // Check if this is a database backup file (.bak extension)
                        bool isDatabaseBackup = path.EndsWith(".bak", StringComparison.OrdinalIgnoreCase);

                        // Also check for trigger file
                        string triggerPath = Path.Combine(env.WebRootPath, path.TrimStart('/') + ".download");
                        bool hasTriggerFile = File.Exists(triggerPath);

                        // Force download for database backups or if explicitly requested
                        if (isDownload || isDatabaseBackup || hasTriggerFile)
                        {
                            // Set content disposition to attachment to force download
                            ctx.Context.Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{filename}\"");

                            // No caching for downloads
                            ctx.Context.Response.Headers.Append("Cache-Control", "no-cache, no-store");
                            ctx.Context.Response.Headers.Append("Pragma", "no-cache");
                            ctx.Context.Response.Headers.Append("Expires", "0");
                        }
                    }
                }
            });

            return app;
        }

        //public static async Task<IApplicationBuilder> UseSeeder(this IApplicationBuilder app)
        //{
        //    using (var scope = app.ApplicationServices.CreateScope())
        //    {
        //        var dataContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        //        var encryptcontext = scope.ServiceProvider.GetRequiredService<IEncryptionServices>();// Make sure AppData is your context type
        //        var seeder = new Seeder(dataContext , encryptcontext);

        //        await seeder.SeedSuperAdmin("SuperAdmin", "AdminSuper"); // You can add a password for the SuperAdmin
        //    }

        //    return app;
        //}

        // Add this method to your existing AppBuilderExtensions class
        //public static IApplicationBuilder UsePrinterWebSockets(this IApplicationBuilder app)
        //{
        //    app.UseWebSockets(new WebSocketOptions
        //    {
        //        KeepAliveInterval = TimeSpan.FromMinutes(2)
        //    });

        //    app.UseMiddleware<PrinterWebSocketMiddleware>();

        //    return app;
        //}
    }
}
