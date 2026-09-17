using Microsoft.AspNetCore.Mvc;
using CLDV7112_Project1_AzureStorageSolution.Services;

namespace CLDV7112_Project1_AzureStorageSolution.Controllers;

public class FilesController : Controller
{
    private readonly AzureStorageService _storage;

    public FilesController(AzureStorageService storage)
    {
        _storage = storage;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            return View(await _storage.GetFilesAsync());
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(new List<Models.FileItemViewModel>());
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Please select a file to upload.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await using var stream = file.OpenReadStream();
            await _storage.UploadFileAsync(stream, file.FileName);
            TempData["Success"] = $"'{file.FileName}' uploaded to Azure File Storage.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Download(string name)
    {
        var result = await _storage.DownloadFileAsync(name);
        return result is null ? NotFound() : File(result.Value.Stream, "application/octet-stream", result.Value.FileName);
    }
}
