using System;
using System.IO;
using System.Reflection;

namespace s3_mover
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("S3 Mover ");

            var watcher = new FileSystemWatcher();
            watcher.Path = args.Length > 0 ? args[0] : Environment.CurrentDirectory;
            watcher.NotifyFilter =  NotifyFilters.LastWrite;
            watcher.Filter = "*.webm";
            watcher.Changed += OnChanged;
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Watching folder " + watcher.Path);
            Console.ReadKey();
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"{e.Name} {e.ChangeType}");
        }
    }
}
