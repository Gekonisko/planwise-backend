using System.Data.Common;
using Npgsql;
using PlanWise.Modules.IdentityAccess.Application.Abstractions.Data;

namespace PlanWise.Modules.IdentityAccess.Infrastructure.Data;

internal sealed class DbConnectionFactory(NpgsqlDataSource dataSource) : IDbConnectionFactory
{
    public async ValueTask<DbConnection> OpenConnectionAsync()
    {
        return await dataSource.OpenConnectionAsync();
    }
}
