using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

class FileWatcher
{
    public string Filter { get; set; }
    public string Path { get; set; }

    public event FileSystemEventHandler Changed;

    private int numberOfFiles;

    /// <summary>
    /// Notice
    /// This implementation wil lose files if 2 or more are created into a single second.
    /// If the process blocks the access to the file it may lose other files created into the 10 seconds.
    /// FileSystemWatcher wont have these issues, if it is not possbile to make it work so it is needed
    /// this current implementation to detect multiple file changes.
    /// </summary>
    public FileWatcher()
    {
        Task.Run(this.CheckForFileChanges);
    }

    private async void CheckForFileChanges()
    {
        var directory = new DirectoryInfo(this.Path);
        var numberOfFiles = directory.GetFiles(this.Filter).Count();

        while (true)
        {
            var files = directory.GetFiles(this.Filter);
            var newNumberOfFiles = files.Count();

            if (newNumberOfFiles > numberOfFiles)
            {
                var file = files.OrderByDescending(f => f.LastWriteTime).First();
                await this.WaitUntilFileNotLocked(file);
                this.Changed!.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Created, file.Directory.FullName, file.Name));
            }

            numberOfFiles = newNumberOfFiles;
        }
    }

    private async Task WaitUntilFileNotLocked(FileInfo file)
    {
        async Task PostPone()
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            await WaitUntilFileNotLocked(file);
        }

        try
        {
            using (FileStream fs = file.Open(FileMode.Open, FileAccess.Write, FileShare.None))
            {
                if (!fs.CanWrite)
                {
                    Console.WriteLine("Cannot Write");
                    fs.Dispose();
                    await PostPone();
                    return;
                }

                long oldLength = 0;
                long newLength = fs.Length;

                while (newLength != oldLength)
                {
                    oldLength = newLength;
                    await Task.Delay(TimeSpan.FromSeconds(1));
                    newLength = fs.Length;
                }
            }
        }
        catch (IOException)
        {
            Console.WriteLine("Exception trying to read");
            await PostPone();
        }
    }

}