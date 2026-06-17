namespace GameStore.PL.Pages.Admin
{
    public class ManageCategoriesModel : PageModel
    {
        private readonly ICategoryService _categoryService;

        public ManageCategoriesModel(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        public List<Category> Categories { get; set; } = new();
        public string? Message  { get; set; }
        public bool    IsError  { get; set; }

        private async Task LoadData()
        {
            Categories = await _categoryService.GetAllWithGameCountAsync();
        }

        public async Task<IActionResult> OnGet()
        {
            await LoadData();
            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync(string Name)
        {
            var (success, error) = await _categoryService.CreateAsync(Name);
            IsError = !success;
            Message = success ? $"Category '{Name}' added successfully." : error;
            await LoadData();
            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync(string Id, string Name)
        {
            await _categoryService.UpdateAsync(Id, Name);
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            await _categoryService.DeleteAsync(id);
            return RedirectToPage();
        }
    }
}
