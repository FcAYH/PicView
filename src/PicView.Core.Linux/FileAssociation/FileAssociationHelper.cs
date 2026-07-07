namespace PicView.Core.Linux.FileAssociation;

public static class FileAssociationHelper
{
    public static Task<bool> RegisterFileAssociation(string extension, string contentType)
    {
        throw new NotImplementedException(); //TODO: Add Linux support
    }

    public static Task<bool> UnregisterFileAssociationMacOS(string extension)
    {
        throw new NotImplementedException(); //TODO: Add Linux support
    }
}
