namespace PicView.Core.Linux.FileAssociation;

public class AssociatedApp
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
}

public static class GetAssociatedFiles
{
    public static List<string> Get()
    {
        throw new NotImplementedException(); //TODO: Add Linux support
    }

    public static async Task<List<AssociatedApp>> GetAssociatedFilesAsync(string path)
    {
        throw new NotImplementedException(); //TODO: Add Linux support
    }
}
