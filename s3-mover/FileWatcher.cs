using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using s3_mover;

class FileWatcher
{
    private const int WaitForFileChangeInSeconds = 5;
    private const int MaxFailedChecks = 6;
    public string Filter
    {
        get => filter;
        set
        {
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
    /// If the process blocks the access to the file it may lose other files created into the 5 seconds.
    /// FileSystemWatcher detects multiple last file access on docker, if that is fixed use it.
    /// this current implementation to detect multiple file changes.
    /// </summary>
    public FileWatcher(ILogger<Worker> logger)
    {
        this.logger = logger;
    }

    private void RunIfReady()
    {
        if(string.IsNullOrEmpty(this.Path) || string.IsNullOrEmpty(this.Filter)) return;

        var task = Task.Run(this.CheckForNewFiles);
        this.logger.LogInformation($"Task scheduled with id {task.Id}, current thread {Thread.CurrentThread.ManagedThreadId}");
    }

    private async void CheckForNewFiles()
    {
        this.logger.LogInformation($"CheckForFileChanges in {this.Path} on thread " + Thread.CurrentThread.ManagedThreadId);

        try
        {
            var directory = new DirectoryInfo(this.Path);
            var numberOfFiles = directory.GetFiles(this.Filter).Count();
            this.logger.LogDebug("checking for new files");

            while(true)
            {
                var files = directory.GetFiles(this.Filter);
                var newNumberOfFiles = files.Count();

                // TODO detect multiple file changes as well as deleted, maybe we need to map in memory the current files and compare against new map
                if(newNumberOfFiles > numberOfFiles)
                {
                    var file = files.OrderByDescending(f => f.LastWriteTime).First();
                    this.logger.LogInformation($"New file detected {file}");
                    this.NotifyWhenFileNotLockedAsync(file);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(WaitForFileChangeInSeconds));
                }

                numberOfFiles = newNumberOfFiles;
            }
        }
        catch(System.Exception e)
        {
            this.logger.LogError("Error on FileWatcher " + e.Message);
            throw;
        }
    }

    private async void NotifyWhenFileNotLockedAsync(FileInfo file)
    {
        await this.WaitUntilFileNotLocked(file);
        this.Changed!.Invoke(this, new FileSystemEventArgs(WatcherChangeTypes.Created, file.Directory.FullName, file.Name));
    }

    private async Task WaitUntilFileNotLocked(FileInfo file)
    {
        async Task PostPone()
        {
            await Task.Delay(TimeSpan.FromSeconds(WaitForFileChangeInSeconds));
            await WaitUntilFileNotLocked(file);
        }

        try
        {
            using(FileStream fs = file.Open(FileMode.Open, FileAccess.Write, FileShare.None))
            {
                if(!fs.CanWrite)
                {
                    this.logger.LogWarning($" {file.Name} Cannot Write");
                    fs.Dispose();
                    await PostPone();
                    return;
                }

                long oldLength = 0;
                long newLength = fs.Length;

                int totalFailedChecks = 0;
                do
                {
                    this.logger.LogDebug($" {file.Name} oldLength {oldLength}, newLength {newLength}");
                    oldLength = newLength;
                    await Task.Delay(TimeSpan.FromSeconds(WaitForFileChangeInSeconds));
                    newLength = fs.Length;
                    if(newLength == 0)
                    {
                        this.logger.LogWarning($" {file.Name} File has 0 bytes after wait {WaitForFileChangeInSeconds} seconds");
                        totalFailedChecks++;
                    }
                } while((newLength != oldLength || newLength == 0) && totalFailedChecks < MaxFailedChecks);

                if(totalFailedChecks == MaxFailedChecks) this.logger.LogError($" {file.Name} File check reported 0 bytes changes during {WaitForFileChangeInSeconds * MaxFailedChecks} seconds");
            }
        }
        catch(IOException e)
        {
            this.logger.LogWarning($"Exception with {file.Name} {e.Message}");
            await PostPone();
        }
        catch(Exception e)
        {
            this.logger.LogWarning($"Exception with {file.Name} {e.Message}");
        }
    }

}