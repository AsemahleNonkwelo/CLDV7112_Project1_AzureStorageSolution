using Microsoft.AspNetCore.Mvc;
using CLDV7112_Project1_AzureStorageSolution.Models;
using CLDV7112_Project1_AzureStorageSolution.Services;

namespace CLDV7112_Project1_AzureStorageSolution.Controllers;

public class CustomersController : Controller
{
    private readonly AzureStorageService _storage;

    public CustomersController(AzureStorageService storage)
    {
        _storage = storage;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            return View(await _storage.GetCustomersAsync());
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(new List<CustomerProfile>());
        }
    }

    public IActionResult Create() => View(new CustomerProfile());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CustomerProfile customer)
    {
        if (!ModelState.IsValid)
            return View(customer);

        try
        {
            await _storage.AddCustomerAsync(customer);
            TempData["Success"] = "Customer profile stored in Azure Table Storage.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
            return View(customer);
        }
    }

    public async Task<IActionResult> Delete(string id)
    {
        var customer = await _storage.GetCustomerAsync(id);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        try
        {
            await _storage.DeleteCustomerAsync(id);
            TempData["Success"] = "Customer profile deleted from Azure Table Storage.";
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
