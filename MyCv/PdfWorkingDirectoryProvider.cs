using MyCv.Export.Pdf;

namespace MyCv;

public class PdfWorkingDirectoryProvider (IWebHostEnvironment webHostEnvironment): IWorkingDirectoryProvider
{
    public string WorkingDirectory => Path.Join(webHostEnvironment.WebRootPath, "data");
}