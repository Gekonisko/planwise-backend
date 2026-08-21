using System.Reflection;

namespace PlanWise.Modules.WorkspaceManagement.Application;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}