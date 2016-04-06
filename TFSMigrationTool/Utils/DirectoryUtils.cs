using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFSMigrationTool.Utils
{
    public static class DirectoryUtils
    {
        public static int CountFiles(string dir)
        {
            DirectoryInfo di = new DirectoryInfo(dir);
            return di.GetFiles("*", SearchOption.AllDirectories).Count();
        }
        public static void CloneDirectory(string sourceDirectory, string targetDirectory, Action<string> incrementcallback = null)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CloneDirectory(diSource, diTarget, incrementcallback);
        }

        public static void CloneDirectory(DirectoryInfo source, DirectoryInfo target, Action<string> incrementcallback = null)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
                incrementcallback?.Invoke(fi.FullName);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                if (diSourceSubDir.FullName.Contains("$tf")) continue;
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CloneDirectory(diSourceSubDir, nextTargetSubDir,incrementcallback);
            }
        }
        public static string GetLocalPath(string remotepath, string remoteroot, string localroot)
        {
            string remotepart = remotepath.Replace(remoteroot, "").Replace('/', '\\');
            if (remotepart.IndexOf('\\') == 0){
                remotepart = remotepart.Substring(1);
            }
            string newPath = Path.Combine(localroot, remotepart);
            Console.WriteLine(newPath);
            return newPath;
        }

        public static string CreateTFSPathFromSource(string targetRoot, string sourceRoot, string path)
        {
            //merge paths
            string newpath = path.Replace(sourceRoot, "");
            if(newpath.IndexOf("/") == 0)
            {
                newpath = newpath.Substring(1);
            }
            if (targetRoot.LastIndexOf("/") == targetRoot.Length - 1)
            {
                newpath = targetRoot + newpath;
            }
            else
            {
                newpath = targetRoot + "/" +  newpath;
            }
            //Console.WriteLine(newpath);
            return newpath;
        }
    }
}
