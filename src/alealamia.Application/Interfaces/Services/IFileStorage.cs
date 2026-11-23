namespace alealamia.Application.Interfaces.Services
{
    public interface IFileStorage
    {
        Task<string> SaveAsync(Stream content, string storedFileName);

        Task DeleteAsync(string storedFileName);
    }
}