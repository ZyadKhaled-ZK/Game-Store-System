namespace GameStore.PL.Models.Orders;

public class OrderDetailViewModel
{
    public Order Order { get; set; } = null!;
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
}
