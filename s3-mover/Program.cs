using System;
using System.IO;
using System.Reflection;

namespace s3_mover
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("S3 Mover");
            Console.WriteLine("Running on " +  Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
            var watcher = new FileSystemWatcher();
            watcher.Path = args == null && args.Length > 0 ? args[0] : ".";
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                   | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = "*.webm";
            watcher.Changed += OnChanged;
            watcher.EnableRaisingEvents = true;

            Console.ReadKey();
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"{e.Name} {e.FullPath}");
        }
    }
}
