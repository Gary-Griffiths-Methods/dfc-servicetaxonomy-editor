using System.IO;
using System.IO.Compression;
using System.Text;

namespace GetJobProfiles
{
    public static class ImportRecipe
    {
        public static void Create(string filename, string contents)
        {
            var zip = GetZipArchive(new InMemoryFile
            {
                FileName = "recipe.json",
                Content = contents
            });


            File.WriteAllBytes(filename, zip);
        }

        private static byte[] GetZipArchive(InMemoryFile file)
        {
            byte[] archiveFile;
            using (var archiveStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(archiveStream, ZipArchiveMode.Create, true))
                {
                    var zipArchiveEntry = archive.CreateEntry(file.FileName, CompressionLevel.Fastest);
                    using var zipStream = zipArchiveEntry.Open();
                    var contentBytes = Encoding.ASCII.GetBytes(file.Content);
                    zipStream.Write(contentBytes, 0, contentBytes.Length) ;
                }

                archiveFile = archiveStream.ToArray();
            }

            return archiveFile;
        }
    }

    public class InMemoryFile
    {
        public string FileName { get; set; }
        public string Content { get; set; }
    }
}
