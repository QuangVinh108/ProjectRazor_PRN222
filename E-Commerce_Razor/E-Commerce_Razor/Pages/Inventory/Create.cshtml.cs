using BLL.DTOs.InventoryDTOs;
using BLL.IService;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

namespace E_Commerce_Razor.Pages.Inventory;
public class CreateModel : PageModel
{
    private readonly IInventoryService _service;

    public CreateModel(IInventoryService service)
    {
        _service = service;
    }

    [BindProperty]
    public CreateInventoryDto Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var result = await _service.CreateAsync(Input);

        if (!result.IsSuccess)
        {
            ModelState.AddModelError("", result.Message);
            return Page();
        }

        return RedirectToPage("Index");
    }
}