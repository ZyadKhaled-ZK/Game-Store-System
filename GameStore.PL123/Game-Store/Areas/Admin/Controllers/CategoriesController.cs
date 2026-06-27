using System.ComponentModel.DataAnnotations;
using GameStore.PL.Models.Admin;

namespace GameStore.PL.Areas.Admin.Controllers;

[Area("Admin")]
[ServiceFilter(typeof(AdminOnlyFilter))]
public class CategoriesController : Controller
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var model = new ManageCategoriesViewModel();
        if (TempData.TryGetValue("Message", out var msg)) model.Message = msg?.ToString();
        if (TempData.TryGetValue("IsError", out var err)) model.IsError = err is bool b && b;
        model.Categories = await _categoryService.GetAllWithGameCountAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Required, StringLength(100)] string Name)
    {
        if (!ModelState.IsValid)
        {
            TempData["Message"] = "Category name is required.";
            TempData["IsError"] = true;
            return RedirectToAction("Index");
        }

        var (success, error) = await _categoryService.CreateAsync(Name);
        TempData["Message"] = success ? $"Category '{Name}' added successfully." : error;
        TempData["IsError"] = !success;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([Required] string Id, [Required, StringLength(100)] string Name)
    {
        if (!ModelState.IsValid) return RedirectToAction("Index");
        var ok = await _categoryService.UpdateAsync(Id, Name);
        TempData["Message"] = ok ? $"Category renamed to '{Name}'." : "Category not found.";
        TempData["IsError"] = !ok;
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete([Required] string id)
    {
        if (!ModelState.IsValid) return RedirectToAction("Index");
        var ok = await _categoryService.DeleteAsync(id);
        TempData["Message"] = ok ? "Category deleted." : "Cannot delete — category has linked games.";
        TempData["IsError"] = !ok;
        return RedirectToAction("Index");
    }
}
