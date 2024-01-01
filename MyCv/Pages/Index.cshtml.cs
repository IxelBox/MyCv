using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using MyCv;
using MyCv.Database;
using MyCv.Model.Providers;

namespace mycv.Pages;

[OutputCache(VaryByRouteValueNames = ["slug"])]
public class IndexModel(
    SideStructure sideStructure,
    IFileExporter exporter,
    MyCvContext myCvContext,
    IConfiguration configuration,
    ApplicationSecretProvider applicationSecretProvider,
    RequestBlocker requestBlocker,
    IMemoryCache memoryCache) : PageModel
{
    public PersonalInformation Personal => sideStructure.PersonalInformation;
    public MyWebCv WebCv => sideStructure.MyWebCv;

    [BindProperty] public UserToken? Login { get; set; }

    public async Task<IActionResult> OnGet(string? slug)
    {
        await myCvContext.Database.EnsureCreatedAsync();

        if (!string.IsNullOrWhiteSpace(slug))
        {
            var side = sideStructure.Sides.FirstOrDefault(s => s.UrlSlug() == slug.ToLower());
            if (side == default) return new ViewResult { ViewName = "NotFound" };

            ViewData["SideData"] = side;
            return new ViewResult { ViewName = "OtherSide", ViewData = ViewData };
        }

        return Page();
    }

    [OutputCache(NoStore = true)]
    public async Task<IActionResult> OnPost()
    {
        if (requestBlocker.BlockRequest(Request)) return BadRequest();

        if (!ModelState.IsValid || Login == null) return Page();

        var adminSection = configuration.GetSection("Admin");
        if (adminSection.Exists())
        {
            var user = adminSection.GetValue<string>("User");
            var token = adminSection.GetValue<string>("Token");

            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(token))
            {
                UserToken adminToken = new(user, token);
                if (Login == adminToken)
                {
                    var adminJwt = CreateToken();
                    return RedirectToPage("Download/Admin", new { Jwt = adminJwt });
                }
            }
        }

        var usedToken =
            await myCvContext.Tokens.FirstOrDefaultAsync(t => t.User == Login!.User && t.Token == Login!.Token);

        if (usedToken == null)
        {
            ModelState.AddModelError("Login.User", "No download token found");
            return Page();
        }

        if (usedToken.UsageCount >= 10)
        {
            ModelState.AddModelError("Login.User", "To many token used");
            return Page();
        }

        if (usedToken.IsExpired())
        {
            ModelState.AddModelError("Login.User", "Token to old");
            return Page();
        }

        usedToken.UsageCount += 1;
        myCvContext.Update(usedToken);
        await myCvContext.SaveChangesAsync();

        var file = await memoryCache.GetOrCreateAsync("cv", async _ =>
        {
            using var memoryStream = new MemoryStream();
            await Task.Run(() => exporter.Create(sideStructure, memoryStream));
            memoryStream.Seek(0, 0);
            var file = memoryStream.ToArray();
            return file;
        });

        return File(file!, "application/pdf", $"Lebenslauf_{sideStructure.PersonalInformation.Name}.pdf");
    }

    private string CreateToken()
    {
        List<Claim> claims = [new Claim(ClaimTypes.Role, "Admin")];

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            applicationSecretProvider.ApplicationSecret));
        var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: cred
        );
        var jwt = new JwtSecurityTokenHandler().WriteToken(token) ??
                  throw new NullReferenceException("token can't be write!");

        return jwt;
    }

    public record UserToken([MaxLength(25)] string User, [MaxLength(25)] string Token);
}