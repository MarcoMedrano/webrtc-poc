using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using s3_mover;

class FileWatcher
{
    public string Filter
    {
        get => filter; 
        set {
            filter = value;
            this.RunIfReady();
        }
    }
    public string Path
    {
        get { return this.path; }
        set
        {
            this.path = value;
            this.RunIfReady();
        }
    }

    public event FileSystemEventHandler Changed;

    private string path;

    private int numberOfFiles;
    private ILogger<Worker> logger;
    private string filter;

    /// <summary>
    /// Notice
    /// This implementation wil lose files if 2 or more are created into 2 seconds.
    /// If the process blocks the access to the file it may lose other files created into the 10 seconds.
    /// FileSystemWatcher detects multiple last file access on docker, if that is fixed use it.
    /// this current implementation to detect multiple file changes.
    /// </summary>
    public FileWatcher(ILogger<Worker> logger)
    {
        this.logger = logger;
    }

    private void RunIfReady()
    {
        if (string.IsNullOrEmpty(this.Path) || string.IsNullOrEmpty(this.Filter)) return;

        var task = Task.Run(this.CheckForFileChanges);
        this.logger.LogInformation($"Task scheduled with id {task.Id}, current thread {Thread.CurrentThread.ManagedThreadId}");
    }

    private async void CheckForFileChanges()
    {
        this.logger.LogInformation($"CheckForFileChanges in {this.Path} on thread " + Thread.CurrentThread.ManagedThreadId);

        try
        {
            var directory = new DirectoryInfo(this.Path);
            var numberOfFiles = directory.GetFiles(this.Filter).Count();

            while (true)
            {
                this.logger.LogDebug("checking for new files");
                var files = directory.GetFiles(this.Filter);
                var newNumberOfFiles = files.Count();

                if (newNumberOfFiles > numberOfFiles)
                {
                    var file = files.OrderByDescending(f => f.LastWriteTime).First();
                    await this.WaitUntilFileNotLocked(file);
                    this.Changed!.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Created, file.Directory.FullName, file.Name));
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(10));
                }

                numberOfFiles = newNumberOfFiles;
            }
        }
        catch (System.Exception e)
        {
            this.logger.LogError("Error on FileWatcher " + e.Message);
            throw;
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
                    this.logger.LogWarning("Cannot Write");
                    fs.Dispose();
                    await PostPone();
                    return;
                }

                long oldLength = 0;
                long newLength = fs.Length;

                do
                {
                    this.logger.LogDebug($"oldLength {oldLength}, newLength {newLength}");
                    oldLength = newLength;
                    await Task.Delay(TimeSpan.FromSeconds(5));
                    newLength = fs.Length;
                } while (newLength != oldLength);
            }
        }
        catch (IOException)
        {
            this.logger.LogWarning("Exception trying to read");
            await PostPone();
        }
    }

}