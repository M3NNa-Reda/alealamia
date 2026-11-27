namespace alealamia.Application.Interfaces.Services
{
    public interface IFileStorage
    {
        Task<string> SaveAsync(Stream content, string storedFileName);

        /// <summary>Opens a stored file for reading, or returns null when it no longer exists.</summary>
        Stream? OpenRead(string storedFileName);

        Task DeleteAsync(string storedFileName);
    }
}
