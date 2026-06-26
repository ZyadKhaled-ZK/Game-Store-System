using System.ComponentModel.DataAnnotations;

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
            if (TempData.TryGetValue("Message", out var msg)) Message = msg?.ToString();
            if (TempData.TryGetValue("IsError", out var err)) IsError = err is bool b && b;
            await LoadData();
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostAddAsync([Required, StringLength(100)] string Name)
        {
            if (!ModelState.IsValid) { IsError = true; Message = "Category name is required."; await LoadData(); return Page(); }
            var (success, error) = await _categoryService.CreateAsync(Name);
            IsError = !success;
            Message = success ? $"Category '{Name}' added successfully." : error;
            await LoadData();
            return Page();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostEditAsync([Required] string Id, [Required, StringLength(100)] string Name)
        {
            if (!ModelState.IsValid) return RedirectToPage();
            var ok = await _categoryService.UpdateAsync(Id, Name);
            TempData["Message"] = ok ? $"Category renamed to '{Name}'." : "Category not found.";
            TempData["IsError"] = !ok;
            return RedirectToPage();
        }

        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnPostDeleteAsync([Required] string id)
        {
            if (!ModelState.IsValid) return RedirectToPage();
            var ok = await _categoryService.DeleteAsync(id);
            TempData["Message"] = ok ? "Category deleted." : "Cannot delete — category has linked games.";
            TempData["IsError"] = !ok;
            return RedirectToPage();
        }
    }
}
