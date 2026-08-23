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
using PlanWise.Modules.Scheduling.Application;
using PlanWise.Modules.Scheduling.Domain.Schedule;
using PlanWise.Modules.Scheduling.Infrastructure;
using PlanWise.Modules.Scheduling.Presentation;
using PlanWise.Modules.CostEstimation.Application;
using PlanWise.Modules.CostEstimation.Domain;
using PlanWise.Modules.CostEstimation.Infrastructure;
using PlanWise.Modules.CostEstimation.Presentation;
using PlanWise.Modules.RiskPrediction.Application;
using PlanWise.Modules.RiskPrediction.Domain;
using PlanWise.Modules.RiskPrediction.Infrastructure;
using PlanWise.Modules.RiskPrediction.Presentation;
using PlanWise.Modules.BacklogPrioritisation.Application;
using PlanWise.Modules.BacklogPrioritisation.Domain;
using PlanWise.Modules.BacklogPrioritisation.Infrastructure;
using PlanWise.Modules.BacklogPrioritisation.Presentation;

namespace PlanWise.ArchitectureTests;

internal static class TestResultExtensions
{
    public static void ShouldBeSuccessful(this TestResult testResult) =>
        Assert.Empty(testResult.FailingTypes ?? []);
}

public sealed class ArchitectureTests
{
    // Namespace prefix -> every assembly (Domain/Application/Infrastructure/Presentation) that makes
    // up that module. Used both for the layer-direction checks below and for the pairwise
    // cross-module isolation check: with 7 modules a hand-written NxN matrix (42 checks) is exactly
    // the kind of boilerplate a copy-paste mistake hides in, so Modules_do_not_reference_each_other
    // iterates this table instead of repeating each pair by hand.
    private static readonly Dictionary<string, Assembly[]> ModuleAssembliesByNamespace = new()
    {
        ["PlanWise.Modules.IdentityAccess"] =
        [
            typeof(User).Assembly,
            typeof(PlanWise.Modules.IdentityAccess.Application.AssemblyReference).Assembly,
            typeof(IdentityAccessEndpoints).Assembly,
            typeof(UsersModule).Assembly
        ],
        ["PlanWise.Modules.WorkspaceManagement"] =
        [
            typeof(Project).Assembly,
            typeof(PlanWise.Modules.WorkspaceManagement.Application.AssemblyReference).Assembly,
            typeof(WorkspaceManagementEndpoints).Assembly,
            typeof(WorkspaceManagementModule).Assembly
        ],
        ["PlanWise.Modules.Delivery"] =
        [
            typeof(Sprint).Assembly,
            typeof(PlanWise.Modules.Delivery.Application.AssemblyReference).Assembly,
            typeof(DeliveryEndpoints).Assembly,
            typeof(DeliveryModule).Assembly
        ],
        ["PlanWise.Modules.Scheduling"] =
        [
            typeof(ScheduleErrors).Assembly,
            typeof(PlanWise.Modules.Scheduling.Application.AssemblyReference).Assembly,
            typeof(SchedulingEndpoints).Assembly,
            typeof(SchedulingModule).Assembly
        ],
        ["PlanWise.Modules.CostEstimation"] =
        [
            typeof(CostEstimateErrors).Assembly,
            typeof(PlanWise.Modules.CostEstimation.Application.AssemblyReference).Assembly,
            typeof(CostEstimationEndpoints).Assembly,
            typeof(CostEstimationModule).Assembly
        ],
        ["PlanWise.Modules.RiskPrediction"] =
        [
            typeof(RiskErrors).Assembly,
            typeof(PlanWise.Modules.RiskPrediction.Application.AssemblyReference).Assembly,
            typeof(RiskPredictionEndpoints).Assembly,
            typeof(RiskPredictionModule).Assembly
        ],
        ["PlanWise.Modules.BacklogPrioritisation"] =
        [
            typeof(PriorityErrors).Assembly,
            typeof(PlanWise.Modules.BacklogPrioritisation.Application.AssemblyReference).Assembly,
            typeof(BacklogPrioritisationEndpoints).Assembly,
            typeof(BacklogPrioritisationModule).Assembly
        ]
    };

    [Fact]
    public void IdentityAccess_layers_point_inward() => AssertLayerDirection(ModuleAssembliesByNamespace["PlanWise.Modules.IdentityAccess"]);

    [Fact]
    public void WorkspaceManagement_layers_point_inward() => AssertLayerDirection(ModuleAssembliesByNamespace["PlanWise.Modules.WorkspaceManagement"]);

    [Fact]
    public void Delivery_layers_point_inward() => AssertLayerDirection(ModuleAssembliesByNamespace["PlanWise.Modules.Delivery"]);

    [Fact]
    public void Scheduling_layers_point_inward() => AssertLayerDirection(ModuleAssembliesByNamespace["PlanWise.Modules.Scheduling"]);

    [Fact]
    public void CostEstimation_layers_point_inward() => AssertLayerDirection(ModuleAssembliesByNamespace["PlanWise.Modules.CostEstimation"]);

    [Fact]
    public void RiskPrediction_layers_point_inward() => AssertLayerDirection(ModuleAssembliesByNamespace["PlanWise.Modules.RiskPrediction"]);

    [Fact]
    public void BacklogPrioritisation_layers_point_inward() => AssertLayerDirection(ModuleAssembliesByNamespace["PlanWise.Modules.BacklogPrioritisation"]);

    [Fact]
    public void Modules_do_not_reference_each_other()
    {
        foreach ((string moduleNamespace, Assembly[] assemblies) in ModuleAssembliesByNamespace)
        {
            foreach (string otherNamespace in ModuleAssembliesByNamespace.Keys)
            {
                if (otherNamespace == moduleNamespace)
                {
                    continue;
                }

                Types.InAssemblies(assemblies)
                    .Should()
                    .NotHaveDependencyOn(otherNamespace)
                    .GetResult()
                    .ShouldBeSuccessful();
            }
        }
    }

    private static void AssertLayerDirection(Assembly[] moduleAssemblies)
    {
        Assembly domain = moduleAssemblies[0];
        Assembly application = moduleAssemblies[1];
        Assembly presentation = moduleAssemblies[2];
        Assembly infrastructure = moduleAssemblies[3];

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
