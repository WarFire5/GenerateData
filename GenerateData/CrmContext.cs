using GenerateData.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GenerateData;

public class CrmContext : DbContext
{
    public virtual DbSet<LeadDto> Leads { get; init; } = default;
    public virtual DbSet<AccountDto> Accounts { get; init; } = default;
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(ConfigureDataSource());
        options.UseSnakeCaseNamingConvention();
        options.EnableSensitiveDataLogging();
    }

    private static NpgsqlDataSource ConfigureDataSource()
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder("server=194.87.210.5;Port=5432;database=crm;username=postgres;password=qwe!23");
        dataSourceBuilder.MapEnum<AccountStatus>();
        dataSourceBuilder.MapEnum<Currency>();
        dataSourceBuilder.MapEnum<LeadStatus>();
        var dataSource = dataSourceBuilder.Build();

        return dataSource;
    }
}