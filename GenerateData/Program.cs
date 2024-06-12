using Microsoft.EntityFrameworkCore;

namespace GenerateData;

public static class Program
{
    public static async Task Main()
    {
        await using var context = new CrmContext();
        var accounts = await context.Accounts.ToListAsync();
    }
}