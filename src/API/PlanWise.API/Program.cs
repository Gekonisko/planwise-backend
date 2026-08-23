using PlanWise.Api.Extensions;
using PlanWise.Modules.IdentityAccess.Infrastructure;
using PlanWise.Modules.WorkspaceManagement.Infrastructure;
using PlanWise.Modules.Delivery.Infrastructure;
using PlanWise.Common.Presentation.Endpoints;
using PlanWise.Common.Application;
using PlanWise.Common.Infrastructure;
using Scalar.AspNetCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplication(
    [
        PlanWise.Modules.IdentityAccess.Application.AssemblyReference.Assembly,
        PlanWise.Modules.WorkspaceManagement.Application.AssemblyReference.Assembly,
        PlanWise.Modules.Delivery.Application.AssemblyReference.Assembly
    ]);
builder.Services.AddCommonInfrastructure(builder.Configuration);

builder.Services.AddUserModule(builder.Configuration);
builder.Services.AddWorkspaceManagementModule(builder.Configuration);
builder.Services.AddDeliveryModule(builder.Configuration);

WebApplication app = builder.Build();

if (app.Environment.IsProduction())
{
    app.UseProblemDetailsExceptionHandler();
}

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    app.ApplyMigrations();
    app.Services.ApplyMigrations();
    app.Services.ApplyDeliveryMigrations();
}

app.MapEndpoints();

app.Run();
