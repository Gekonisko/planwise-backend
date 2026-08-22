using PlanWise.Common.Domain;

namespace PlanWise.Common.Application.Exceptions;

public sealed class PlanWiseException : Exception
{
    public PlanWiseException(string requestName, Error? error = default, Exception? innerException = default)
        : base("Application exception", innerException)
    {
        RequestName = requestName;
        Error = error;
    }

    public string RequestName { get; }

    public Error? Error { get; }
}
