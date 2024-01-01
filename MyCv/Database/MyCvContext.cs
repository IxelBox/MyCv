using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace MyCv.Database;

public class MyCvContext(IWebHostEnvironment environment) : DbContext
{
    public DbSet<DownloadToken> Tokens { get; set; }

    public string DbPath { get; } = Path.Join(environment.WebRootPath, "data", "MyCv.db");

    // The following configures EF to create a Sqlite database file in the
    // "data" folder for your web environment.
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        options.UseSqlite($"Data Source={DbPath}");
    }
}

public class DownloadToken(string user, string token, string description)
{
    [Key] public int TokenId { get; set; }

    [MaxLength(25)] public string User { get; set; } = user;

    [MaxLength(25)] public string Token { get; set; } = token;

    [MaxLength(300)] public string Description { get; set; } = description;

    public DateTime CreatedDateUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiryDateUtc { get; set; } = DateTime.UtcNow.AddDays(7);
    public int UsageCount { get; set; }
}

public static class TokenExtensions
{
    public static bool IsExpired(this DownloadToken token)
    {
        return token.ExpiryDateUtc < DateTime.UtcNow;
    }
}