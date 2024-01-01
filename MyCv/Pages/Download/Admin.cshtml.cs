using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyCv;
using MyCv.Database;

namespace mycv.Pages.Download;

public class Admin(
    ApplicationSecretProvider applicationSecretProvider,
    RequestBlocker requestBlocker,
    MyCvContext myCvContext) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Jwt { get; set; }

    [BindProperty] public UserToken? DownloadToken { get; set; }

    public DownloadToken[]? Tokens { get; set; }

    public async Task<IActionResult> OnGet()
    {
        if (requestBlocker.BlockRequest(Request)) return BadRequest();

        if (!ModelState.IsValid) return BadRequest();

        if (string.IsNullOrEmpty(Jwt) || !ValidateJwt()) return BadRequest();

        requestBlocker.RemoveBlockCount(Request);

        await FillPageData();

        return Page();
    }

    public async Task<IActionResult> OnPostAdd()
    {
        if (requestBlocker.BlockRequest(Request)) return BadRequest();

        if (!ModelState.IsValid || DownloadToken == null) return BadRequest();

        if (string.IsNullOrEmpty(Jwt) || !ValidateJwt()) return BadRequest();
        requestBlocker.RemoveBlockCount(Request);

        DownloadToken token = new(DownloadToken.User.Trim(), DownloadToken.Token.Trim(),
            DownloadToken.Description.Trim());
        await myCvContext.Tokens.AddAsync(token);
        await myCvContext.SaveChangesAsync();

        await FillPageData();

        return Page();
    }

    private async Task FillPageData()
    {
        Tokens = await myCvContext.Tokens.OrderBy(t => t.User).ThenBy(t => t.CreatedDateUtc).ToArrayAsync();
    }

    private bool ValidateJwt()
    {
        try
        {
            new JwtSecurityTokenHandler().ValidateToken(Jwt, GetValidationParameters(), out var _);
        }
        catch (Exception)
        {
            return false;
        }

        return true;
    }

    private TokenValidationParameters GetValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateLifetime = true, // Because there is no expiration in the generated token
            ValidateAudience = false, // Because there is no audiance in the generated token
            ValidateIssuer = false, // Because there is no issuer in the generated token
            IssuerSigningKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(applicationSecretProvider
                        .ApplicationSecret)) // The same key as the one that generate the token
        };
    }

    public record UserToken(
        [MaxLength(25)] string User,
        [MaxLength(25)] string Token,
        [MaxLength(300)] string Description);
}