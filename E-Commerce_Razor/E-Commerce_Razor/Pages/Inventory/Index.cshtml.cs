using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BLL.IService;
using BLL.DTOs.InventoryDTOs;

namespace E_Commerce_Razor.Pages.Inventory;

public class IndexModel : PageModel
{
    private readonly IInventoryService _service;

    public IndexModel(IInventoryService service)
    {
        _service = service;
    }

    public PagedResult<InventoryDto>? Result { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;
    [BindProperty(SupportsGet = true)]
    public int PageSize { get; set; } = 10;

    public async Task OnGetAsync()
    {
        var query = new QueryInventoryDTO
        {
            Search = Search,
            PageIndex = PageIndex,
            PageSize = PageSize
        };

        var response = await _service.GetAllAsync(query);
        Result = response.Data;
    }

    public async Task<IActionResult> OnPostDeleteAsync(int productId)
    {
        var result = await _service.DeleteAsync(productId);

        if (result.IsSuccess)
            TempData["Success"] = "Xóa thành công";
        else
            TempData["Error"] = result.Message;

        return RedirectToPage();
    }
}