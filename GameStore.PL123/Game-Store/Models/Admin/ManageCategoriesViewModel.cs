namespace GameStore.PL.Models.Admin;

public class ManageCategoriesViewModel
{
    public List<Category> Categories { get; set; } = new();
    public string? Message { get; set; }
    public bool IsError { get; set; }
}
