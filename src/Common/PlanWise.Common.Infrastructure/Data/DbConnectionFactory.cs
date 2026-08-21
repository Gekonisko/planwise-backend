using System.Data.Common;
using Npgsql;
using PlanWise.Common.Application.Data;

namespace PlanWise.Common.Infrastructure.Data;

internal sealed class DbConnectionFactory(NpgsqlDataSource dataSource) : IDbConnectionFactory
{
    public async ValueTask<DbConnection> OpenConnectionAsync() =>
        await dataSource.OpenConnectionAsync();
}