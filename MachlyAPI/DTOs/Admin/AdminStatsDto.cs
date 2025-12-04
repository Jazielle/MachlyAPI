namespace MachlyAPI.DTOs.Admin;

public class AdminStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalProviders { get; set; }
    public int TotalRenters { get; set; }
    public int TotalMachines { get; set; }
    public int TotalBookings { get; set; }
    public int PendingVerifications { get; set; }
    public decimal TotalRevenue { get; set; }
}

public class VerifyProviderDto
{
    public bool Verified { get; set; }
}
