using GenerateData.Enums;

namespace GenerateData;

public class AccountDto: IdContainer
{
    public Currency Currency { get; init; }
    public AccountStatus Status { get; set; }
    public LeadDto Lead { get; set; }
}