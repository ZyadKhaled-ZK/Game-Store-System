namespace GameStore.PL.Models.Admin;

public class ManageUsersViewModel
{
    public List<User> Users { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public bool IsError { get; set; }
}
