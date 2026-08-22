using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Npgsql;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Authentication;
using PlanWise.Common.Application.Data;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Data;
using PlanWise.Modules.IdentityAccess.Application.Services;
using PlanWise.Modules.IdentityAccess.Domain.Users;
using PlanWise.Modules.IdentityAccess.Domain.Abstractions;
using PlanWise.Modules.IdentityAccess.Infrastructure.Database;
using PlanWise.Modules.IdentityAccess.Infrastructure.Users;
using PlanWise.Modules.IdentityAccess.Infrastructure.Authentication;
using PlanWise.Modules.IdentityAccess.Presentation.Authentication;
using PlanWise.Modules.IdentityAccess.Presentation;
using PlanWise.Common.Presentation.Endpoints;
using PlanWise.Modules.IdentityAccess.Presentation.Users;

namespace PlanWise.Modules.IdentityAccess.Infrastructure;

public static class UsersModule
{
    public static IServiceCollection AddUserModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddEndpoints(typeof(IdentityAccessEndpoints).Assembly);

        services.AddOptions<JwtOptions>()
            .BindConfiguration(JwtOptions.SectionName)
            .Validate(options => Encoding.UTF8.GetByteCount(options.SecretKey) >= 32,
                "Authentication:Jwt:SecretKey must be at least 32 bytes")
            .ValidateOnStart();
        services.AddOptions<PasswordResetOptions>().BindConfiguration(PasswordResetOptions.SectionName);

        services.AddHttpContextAccessor();
        services.AddScoped<IUserContext, UserContext>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddSingleton<IPasswordResetSender, LoggingPasswordResetSender>();
        services.AddScoped<AuthenticationService>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                JwtOptions jwtOptions = configuration
                    .GetSection(JwtOptions.SectionName)
                    .Get<JwtOptions>() ?? new JwtOptions();

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });
        services.AddAuthorization();

        services.AddInfrastructure(configuration);

        return services;
    }

    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        string databaseConnection = configuration.GetConnectionString("Database");

        NpgsqlDataSource npgsqlDataSource = new NpgsqlDataSourceBuilder(databaseConnection).Build();
        services.TryAddSingleton(npgsqlDataSource);


        services.AddDbContext<IdentityAccessDbContext>((serviceProvider, options) =>
            options.UseNpgsql(
                databaseConnection,
                npqsqloptions => npqsqloptions.MigrationsHistoryTable(HistoryRepository.DefaultTableName, Schemas.IdentityAccess))
            .UseSnakeCaseNamingConvention());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IdentityAccessDbContext>());

        services.AddSingleton<IPasswordHasher, PasswordHasher>();
    }
}
