using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using EPC.Infrastructure.Identity;


namespace EPC.WEB.Pages.Account;


public class LogoutModel : PageModel
{
    private readonly SignInManager<AppUser> _signInManager;


    public LogoutModel(SignInManager<AppUser> signInManager)
    {
        _signInManager = signInManager;
    }


    public async Task<IActionResult> OnPost()
    {
        await _signInManager.SignOutAsync();
        return RedirectToPage("/Account/Login");
    }
}