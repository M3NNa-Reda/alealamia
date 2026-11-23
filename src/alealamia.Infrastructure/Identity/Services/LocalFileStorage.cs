using alealamia.Application.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;

namespace alealamia.Infrastructure.Identity.Services
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _uploadsPath;

        public LocalFileStorage(IWebHostEnvironment env)
        {
            var root = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
            _uploadsPath = Path.Combine(root, "uploads");
        }

        public async Task<string> SaveAsync(Stream content, string storedFileName)
        {
            Directory.CreateDirectory(_uploadsPath);

            var path = Path.Combine(_uploadsPath, Path.GetFileName(storedFileName));

            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                await content.CopyToAsync(fileStream);
            }

            return "/uploads/" + storedFileName;
        }

        public Task DeleteAsync(string storedFileName)
        {
            var path = Path.Combine(_uploadsPath, Path.GetFileName(storedFileName));

            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);

            return Task.CompletedTask;
        }
    }

}
