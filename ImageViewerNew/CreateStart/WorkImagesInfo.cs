using ImageViewerNew.DTO;
using ImageViewerNew.Utils;
using System.IO;
using System.Text.Json;

namespace ImageViewerNew.CreateStart
{
    // Класс создания файла с информацией для передачи в клиентскую часть (файл: images.js)
    public class WorkImagesInfo
    {
        /// <summary>
        /// sourcePath - Путь к папке с файлами ;
        /// currentFilePath - Путь к выбранному файлу ;
        /// </summary>
        public void CreateFile(string sourcePath, string? currentFilePath, string host)
        {
            // ------- путь для файла с путями к картинкам -------
            var wwwPath = Path.Combine(AppContext.BaseDirectory, "wwwroot");
            var jsPath = Path.Combine(wwwPath, "images.js");

            // ----- Отсортировать файлы как в Windows ----------------
            var files = GetSortedImageFiles(sourcePath);

            // ----- Найти индекс текущего файла ------------------
            int startIndex = 0;

            if (!string.IsNullOrEmpty(currentFilePath))
            {
                startIndex = files.FindIndex(f =>
                    string.Equals(
                        Path.GetFullPath(f),
                        Path.GetFullPath(currentFilePath),
                        StringComparison.OrdinalIgnoreCase
                    )
                );
            }
            // ----------- end ---------------

            // ------- Создание списка ImageInfo ------------
            var images = files
                .Select(f => new ImageInfo
                {
                    Name = Path.GetFileName(f),
                    Path = Path.Combine($"https://{host}", Path.GetFileName(f))
                })
                .ToList();
            // ----------- end ---------------

            // ------ Генерация images.js с индексом -----------
            var jsContent =
                "window.images = " +
                JsonSerializer.Serialize(images, new JsonSerializerOptions { WriteIndented = true }) +
            ";\n\n" +
                $"window.startIndex = {startIndex};\n\n" +
                $"window.sourcePath = {JsonSerializer.Serialize(sourcePath)};\n\n" +
                $"window.urlServer = {JsonSerializer.Serialize("http://localhost:21235")};";

            File.WriteAllText(jsPath, jsContent);
            // ----------- end ---------------
        }

        // ----- Отсортировать файлы как в Windows ----------------
        public List<string> GetSortedImageFiles(string sourcePath)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".avif" };
            var files = Directory.Exists(sourcePath)
                ? Directory.GetFiles(sourcePath, "*.*")
                    .Where(f => allowedExtensions.Contains(Path.GetExtension(f).ToLower()))
                    .OrderBy(f => Path.GetFileName(f), new WindowsFileSorter())
                    .ToList()
                : new List<string>();
            return files;
        }
    }
}
