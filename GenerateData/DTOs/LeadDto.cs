using GenerateData.Enums;

namespace GenerateData;

public class LeadDto: IdContainer
{
    public string Name { get; set; }
    public string Mail { get; set; }
    public string Phone { get; set; }
    public string Address { get; set; }
    public DateOnly BirthDate { get; set; }
    public LeadStatus Status { get; set; }
    public string Password { get; set; }
    public string Salt { get; set; }
    public string RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
    public bool IsDeleted { get; set; }
    public List<AccountDto> Accounts { get; set; }
}