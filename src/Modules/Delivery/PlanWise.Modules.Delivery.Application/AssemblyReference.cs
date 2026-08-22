using System.Reflection;

namespace PlanWise.Modules.Delivery.Application;

public static class AssemblyReference
{
    public static readonly Assembly Assembly = typeof(AssemblyReference).Assembly;
}
