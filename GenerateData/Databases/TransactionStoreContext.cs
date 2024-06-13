using GenerateData.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Npgsql;

namespace GenerateData;

public class TransactionStoreContext : DbContext
{
    public DbSet<TransactionDto> Transactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresEnum<TransactionType>();
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseNpgsql(ConfigureDataSource());
        options.UseSnakeCaseNamingConvention();
        options.EnableSensitiveDataLogging();
    }
    
    private static NpgsqlDataSource ConfigureDataSource()
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(Options.TsConnectionString);
        dataSourceBuilder.MapEnum<TransactionType>();
        var dataSource = dataSourceBuilder.Build();

        return dataSource;
    }
}