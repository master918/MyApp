using System.IO;
using MyApp.Droid;
using MyApp.Services;
using Xamarin.Forms;
using Android.OS;
using static MyApp.Services.LocalDbService;

[assembly: Dependency(typeof(AndroidFileExportService))]
namespace MyApp.Droid
{
    public class AndroidFileExportService : IFileExportService
    {
        public string ExportDatabase(string sourcePath, string fileName)
        {
            var downloadDir = Environment.GetExternalStoragePublicDirectory(Environment.DirectoryDownloads).AbsolutePath;
            var destPath = Path.Combine(downloadDir, fileName);

            File.Copy(sourcePath, destPath, true);
            return destPath;
        }
    }
}
