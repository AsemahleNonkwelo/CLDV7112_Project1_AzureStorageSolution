using Microsoft.AspNetCore.Mvc;
using CLDV7112_Project1_AzureStorageSolution.Services;

namespace CLDV7112_Project1_AzureStorageSolution.Controllers;

public class QueuesController : Controller
{
    private readonly AzureStorageService _storage;

    public QueuesController(AzureStorageService storage)
    {
        _storage = storage;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            return View(await _storage.PeekQueueAsync());
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(new List<Models.QueueMessageViewModel>());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            TempData["Error"] = "Message cannot be empty.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _storage.SendQueueMessageAsync(message);
            TempData["Success"] = "Message added to Azure Queue Storage.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
