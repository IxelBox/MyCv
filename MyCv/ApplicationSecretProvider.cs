namespace MyCv;

public class ApplicationSecretProvider
{
    public ApplicationSecretProvider()
    {
        ApplicationSecret = string.Join("", Enumerable.Range(1, 10).Select(i => createSecretPart())); 
               }

    private string createSecretPart() => Guid.NewGuid().ToString("N").Substring(0, 8);
    
    public string ApplicationSecret { get; }
}