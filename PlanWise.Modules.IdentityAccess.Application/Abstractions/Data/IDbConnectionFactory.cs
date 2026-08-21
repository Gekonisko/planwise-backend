using System.Data.Common;

namespace PlanWise.Modules.IdentityAccess.Application.Abstractions.Data;

public interface IDbConnectionFactory
{
    ValueTask<DbConnection> OpenConnectionAsync();
}
