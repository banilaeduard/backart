namespace ServiceInterface.Storage
{
    public interface ICryptoService
    {
        string GetMd5(string input);
        int GetStableHashCode(string? str);
    }
}
