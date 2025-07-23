using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nastya_Archiving_project.Helper;
using Nastya_Archiving_project.Models;
using Nastya_Archiving_project.Models.DTOs.ArchivingDocs;
using Nastya_Archiving_project.Services.archivingDocs;
using Nastya_Archiving_project.Services.ArchivingSettings;
using Nastya_Archiving_project.Services.auth;
using Nastya_Archiving_project.Services.encrpytion;
using Nastya_Archiving_project.Services.files;
using Nastya_Archiving_project.Services.infrastructure;
using Nastya_Archiving_project.Services.Permmsions;
using Nastya_Archiving_project.Services.reports;
using Nastya_Archiving_project.Services.search;
using Nastya_Archiving_project.Services.SystemInfo;
using Nastya_Archiving_project.Services.userInterface;
using Nastya_Archiving_project.Services.usersPermission;
using Nastya_Archiving_project.Swagger;
using System.Runtime.CompilerServices;
using System.Text;

namespace Nastya_Archiving_project.Extinstion
{
    public static class AppServicesExtensions
    {
        public static IServiceCollection AddAuthConfig(this IServiceCollection services)
        {
            string jwtSignInKey = ConfigProvider.config.GetSection("Jwt:Key").Get<string>();
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = ConfigProvider.config.GetSection("Jwt:Issure").Get<string>(), // Ensure this matches the issuer in token
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSignInKey))
                };
            });

            services.AddAuthorization();
            return services;
        }
        public static IServiceCollection AddSwaggerConfig(this IServiceCollection services)
        {
            services.AddSwaggerGen(opt =>
            {
                opt.SwaggerDoc(
                    name: "Main",
                    new OpenApiInfo
                    {
                        Version = "v1",
                        Title = "Scorpion Swagger",
                        Description = "Swagger for Scorpion website",
                        Contact = new OpenApiContact
                        {
                            Name = "Samer Adnan",
                            Email = "ssamr1082@gmail.com",
                            Url = new Uri("https://supernova-iq.com/Team/Nexux")
                        }
                    });

                opt.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header, \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer"
                });

                opt.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] { }
                }
            });



                opt.OperationFilter<AcceptHeaderOperationFilter>();
            });

            return services;
        }

        public static IServiceCollection AddServices(this IServiceCollection services)
        {

           services.AddScoped<IAuthServices, AuthServices>();

            //{{INSERTION_POINT}}
            services.AddScoped<IInfrastructureServices, InfrastructureServices>();
            services.AddScoped<IInfrastructureServices, InfrastructureServices>();
            services.AddScoped<IAuthServices, AuthServices>();
            services.AddScoped<ISystemInfoServices, SystemInfoServices>();
            services.AddScoped<IPermissionsServices, PermissionsServices>();
            services.AddScoped<IArchivingSettingsServicers, ArchivingSettingsServices>();
            services.AddScoped<IArchivingDocsSercvices, ArchivingDocsServices>();
            services.AddScoped<IUserInterfaceServices, UserInterfaceServices>();
            services.AddScoped<IEncryptionServices, EncryptionServices>();
            services.AddScoped<IFilesServices, FileServices>();
            services.AddScoped<InfrastructureServices>();
            services.AddScoped<ISearchServices , SearchServices>();
            services.AddScoped<IUserPermissionsServices, UserPermissionServices>();
            services.AddScoped<IReportServices, ResportServices>();
            services.AddHttpContextAccessor();
            services.AddEndpointsApiExplorer();


            return services;
        }
        public static IServiceCollection AddMapperConfig(this IServiceCollection services)
        {
            services.AddAutoMapper(config =>
            {
                config.AddProfile<UserMappingProfile>();

                //{{INSERTION_POINT}}

            });

            return services;
        }

    }
}
