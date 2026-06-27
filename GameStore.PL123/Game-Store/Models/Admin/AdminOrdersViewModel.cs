namespace GameStore.PL.Models.Admin;

public class AdminOrdersViewModel
{
    public List<Order> Orders { get; set; } = new();
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
}
