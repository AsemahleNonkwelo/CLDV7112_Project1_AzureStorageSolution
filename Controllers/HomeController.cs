using Microsoft.AspNetCore.Mvc;
using CLDV7112_Project1_AzureStorageSolution.Services;

namespace CLDV7112_Project1_AzureStorageSolution.Controllers;

public class HomeController : Controller
{
    private readonly AzureStorageService _storage;

    public HomeController(AzureStorageService storage)
    {
        _storage = storage;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _storage.GetDashboardAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Seed()
    {
        try
        {
            await _storage.SeedDemoDataAsync();
            TempData["Success"] = "Demo data created successfully. Each Azure Storage service now has at least five records/files/messages.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
