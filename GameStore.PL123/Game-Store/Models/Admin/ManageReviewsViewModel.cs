namespace GameStore.PL.Models.Admin;

public class ManageReviewsViewModel
{
    public List<Review> Reviews { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
}
