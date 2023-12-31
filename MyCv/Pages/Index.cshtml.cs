using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MyCv;
using MyCv.Database;
using MyCv.Model.Providers;
using mycv.Pages.Download;
using YamlDotNet.Core.Tokens;

namespace mycv.Pages;

public class IndexModel(
    SideStructure sideStructure,
    IFileExporter exporter,
    MyCvContext myCvContext,
    IConfiguration configuration,
    ApplicationSecretProvider applicationSecretProvider,
    RequestBlocker requestBlocker) : PageModel
{
    public PersonalInformation Personal => sideStructure.PersonalInformation;
    public MyWebCv WebCv => sideStructure.MyWebCv;
    
    public async Task<IActionResult> OnGet(string? slug)
    {
        await myCvContext.Database.EnsureCreatedAsync();
        
        if (!string.IsNullOrWhiteSpace(slug))
        {
            var side = sideStructure.Sides.FirstOrDefault(s => s.UrlSlug() == slug.ToLower());
            if (side == default)
            {
                return new ViewResult(){ViewName = "NotFound"};
            }

            ViewData["SideData"] = side;
            return new ViewResult(){ViewName = "OtherSide", ViewData = this.ViewData};

        }

        return this.Page();
    }

    [BindProperty]
    public UserToken Login {
        get;
        set;
    }
    
    public async Task<IActionResult> OnPost()
    {
        if (requestBlocker.BlockRequest(Request))
        {
            return this.BadRequest();
        }
        
        if (!ModelState.IsValid)
        {
            return Page();
        }

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

        var usedToken = await myCvContext.Tokens.FirstOrDefaultAsync(t => t.User == Login.User && t.Token == Login.Token);

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
        
        var memoryStream = new MemoryStream();
        await Task.Run(() => exporter.Create(sideStructure, memoryStream));

        return File(memoryStream, "application/pdf", $"Lebenslauf_{sideStructure.PersonalInformation.Name}.pdf");
    }
    
    private string CreateToken()
    {
        List<Claim> claims = new()
        {
            new Claim(ClaimTypes.Role,"Admin"),
        };
 
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(
            applicationSecretProvider.ApplicationSecret));
        var cred = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: cred
        );
        var jwt = new JwtSecurityTokenHandler().WriteToken(token) ?? throw new NullReferenceException("token can't be write!");
        
        return jwt;
    }
    
    public record UserToken([MaxLength(25)] string User, [MaxLength(25)] string Token);

}
