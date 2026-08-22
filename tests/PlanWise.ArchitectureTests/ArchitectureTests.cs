using System.Reflection;
using NetArchTest.Rules;
using PlanWise.Modules.IdentityAccess.Application;
using PlanWise.Modules.IdentityAccess.Domain.Users;
using PlanWise.Modules.IdentityAccess.Infrastructure;
using PlanWise.Modules.IdentityAccess.Presentation;
using PlanWise.Modules.WorkspaceManagement.Application;
using PlanWise.Modules.WorkspaceManagement.Domain.Projects;
using PlanWise.Modules.WorkspaceManagement.Infrastructure;
using PlanWise.Modules.WorkspaceManagement.Presentation;
using PlanWise.Modules.Delivery.Application;
using PlanWise.Modules.Delivery.Domain.Sprints;
using PlanWise.Modules.Delivery.Infrastructure;
using PlanWise.Modules.Delivery.Presentation;

namespace PlanWise.ArchitectureTests;

internal static class TestResultExtensions
{
    public static void ShouldBeSuccessful(this TestResult testResult) =>
        Assert.Empty(testResult.FailingTypes ?? []);
}

public sealed class ArchitectureTests
{
    [Fact]
    public void IdentityAccess_layers_point_inward()
    {
        AssertLayerDirection(
            typeof(User).Assembly,
            typeof(PlanWise.Modules.IdentityAccess.Application.AssemblyReference).Assembly,
            typeof(IdentityAccessEndpoints).Assembly,
            typeof(UsersModule).Assembly);
    }

    [Fact]
    public void WorkspaceManagement_layers_point_inward()
    {
        AssertLayerDirection(
            typeof(Project).Assembly,
            typeof(PlanWise.Modules.WorkspaceManagement.Application.AssemblyReference).Assembly,
            typeof(WorkspaceManagementEndpoints).Assembly,
            typeof(WorkspaceManagementModule).Assembly);
    }

    [Fact]
    public void Delivery_layers_point_inward()
    {
        AssertLayerDirection(
            typeof(Sprint).Assembly,
            typeof(PlanWise.Modules.Delivery.Application.AssemblyReference).Assembly,
            typeof(DeliveryEndpoints).Assembly,
            typeof(DeliveryModule).Assembly);
    }

    [Fact]
    public void Modules_do_not_reference_each_other()
    {
        string identityAccessNamespace = "PlanWise.Modules.IdentityAccess";
        string workspaceNamespace = "PlanWise.Modules.WorkspaceManagement";
        string deliveryNamespace = "PlanWise.Modules.Delivery";

        Assembly[] identityAccessAssemblies =
        [
            typeof(User).Assembly,
            typeof(PlanWise.Modules.IdentityAccess.Application.AssemblyReference).Assembly,
            typeof(IdentityAccessEndpoints).Assembly,
            typeof(UsersModule).Assembly
        ];

        Assembly[] workspaceAssemblies =
        [
            typeof(Project).Assembly,
            typeof(PlanWise.Modules.WorkspaceManagement.Application.AssemblyReference).Assembly,
            typeof(WorkspaceManagementEndpoints).Assembly,
            typeof(WorkspaceManagementModule).Assembly
        ];

        Assembly[] deliveryAssemblies =
        [
            typeof(Sprint).Assembly,
            typeof(PlanWise.Modules.Delivery.Application.AssemblyReference).Assembly,
            typeof(DeliveryEndpoints).Assembly,
            typeof(DeliveryModule).Assembly
        ];

        Types.InAssemblies(identityAccessAssemblies)
            .Should()
            .NotHaveDependencyOn(workspaceNamespace)
            .GetResult()
            .ShouldBeSuccessful();

        Types.InAssemblies(identityAccessAssemblies)
            .Should()
            .NotHaveDependencyOn(deliveryNamespace)
            .GetResult()
            .ShouldBeSuccessful();

        Types.InAssemblies(workspaceAssemblies)
            .Should()
            .NotHaveDependencyOn(identityAccessNamespace)
            .GetResult()
            .ShouldBeSuccessful();

        Types.InAssemblies(workspaceAssemblies)
            .Should()
            .NotHaveDependencyOn(deliveryNamespace)
            .GetResult()
            .ShouldBeSuccessful();

        Types.InAssemblies(deliveryAssemblies)
            .Should()
            .NotHaveDependencyOn(identityAccessNamespace)
            .GetResult()
            .ShouldBeSuccessful();

        Types.InAssemblies(deliveryAssemblies)
            .Should()
            .NotHaveDependencyOn(workspaceNamespace)
            .GetResult()
            .ShouldBeSuccessful();
    }

    private static void AssertLayerDirection(
        Assembly domain,
        Assembly application,
        Assembly presentation,
        Assembly infrastructure)
    {
        Types.InAssembly(domain)
            .Should()
            .NotHaveDependencyOn(application.GetName().Name!)
            .GetResult()
            .ShouldBeSuccessful();

        Types.InAssembly(domain)
            .Should()
            .NotHaveDependencyOn(infrastructure.GetName().Name!)
            .GetResult()
            .ShouldBeSuccessful();

        Types.InAssembly(application)
            .Should()
            .NotHaveDependencyOn(infrastructure.GetName().Name!)
            .GetResult()
            .ShouldBeSuccessful();

        Types.InAssembly(application)
            .Should()
            .NotHaveDependencyOn(presentation.GetName().Name!)
            .GetResult()
            .ShouldBeSuccessful();

        Types.InAssembly(presentation)
            .Should()
            .NotHaveDependencyOn(infrastructure.GetName().Name!)
            .GetResult()
            .ShouldBeSuccessful();
    }
}
