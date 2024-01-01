namespace MyCv;

public class ApplicationSecretProvider
{
    public ApplicationSecretProvider()
    {
        ApplicationSecret = string.Join("", Enumerable.Range(1, 10).Select(i => createSecretPart()));
    }

    public string ApplicationSecret { get; }

    private string createSecretPart()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 8);
    }
}