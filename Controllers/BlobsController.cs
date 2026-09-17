using Microsoft.AspNetCore.Mvc;
using CLDV7112_Project1_AzureStorageSolution.Services;

namespace CLDV7112_Project1_AzureStorageSolution.Controllers;

public class BlobsController : Controller
{
    private readonly AzureStorageService _storage;

    public BlobsController(AzureStorageService storage)
    {
        _storage = storage;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            return View(await _storage.GetBlobsAsync());
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(new List<Models.BlobItemViewModel>());
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
            await _storage.UploadBlobAsync(stream, file.FileName, file.ContentType);
            TempData["Success"] = $"'{file.FileName}' uploaded to Azure Blob Storage.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Download(string name)
    {
        var result = await _storage.DownloadBlobAsync(name);
        return result is null ? NotFound() : File(result.Value.Stream, result.Value.ContentType, result.Value.FileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string name)
    {
        try
        {
            await _storage.DeleteBlobAsync(name);
            TempData["Success"] = $"'{name}' deleted from Azure Blob Storage.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
