namespace GameStore.PL.Models.Refunds;

public class RefundViewModel
{
    public List<Order> Orders { get; set; } = new();
    public List<RefundRequest> Requests { get; set; } = new();
    public string? Message { get; set; }
    public bool IsError { get; set; }
}

public class RefundRequestInput
{
    public string OrderId { get; set; } = string.Empty;
    public string GameId { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
}
