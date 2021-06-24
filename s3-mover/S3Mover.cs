using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;

/// <summary>
/// This program will upload to S3 the file created in the specified path in arguments.
/// Environemnt variables required:
/// AWS_ACCESS_KEY_ID
/// AWS_SECRET_KEY
/// AWS_BUCKET_NAME
/// 
/// This application can be run as AWS_BUCKET_NAME=<AWS_BUCKET_NAME> AWS_ACCESS_KEY_ID=<AWS_ACCESS_KEY_ID> AWS_SECRET_KEY=<AWS_SECRET_KEY> ./s3-mover watch=/tmp
/// </summary>
namespace s3_mover
{
    class S3Mover
    {
        private const string FILTER = "*.webm";
        private string path = string.Empty;
        private ILogger<Worker> logger;
        private readonly RedisRecordingTracker redis;

        // TODO inject class type logger
        public S3Mover(ILogger<Worker> logger)
        {
            this.logger = logger;
            this.redis = new RedisRecordingTracker(Environment.GetEnvironmentVariable("REDIS_HOSTS"),
                                                    Environment.GetEnvironmentVariable("MS_NAME"),
                                                    logger);
        }

        public void Run(string folder)
        {
            this.logger.LogInformation("S3 Mover 13 " + folder);
            path = !string.IsNullOrEmpty(folder) ? folder : Environment.GetEnvironmentVariable("S3_MOVER_FOLDER") ?? Environment.CurrentDirectory;
            // var watcher2 = new FileSystemWatcher();
            // watcher2.Path = path;
            // watcher2.NotifyFilter = NotifyFilters.LastWrite;
            // watcher2.Filter = FILTER;
            // watcher2.Changed += (object sender, FileSystemEventArgs e) => { this.logger.LogInformation($"FSW {e.Name} {e.ChangeType}"); };
            // watcher2.EnableRaisingEvents = true;

            // FileSystemWatcher not working in Docker image other than the MS one.
            // so had to implement a long pulling file watcher
            var watcher = new FileWatcher(this.logger);
            watcher.Path = path;
            watcher.Filter = FILTER;
            watcher.Changed += OnChanged;
            // Probably is worth try to run kurento into MS image (if MS image is working for the file watcher)

            this.logger.LogInformation("Watching folder " + watcher.Path);
            this.logger.LogInformation("Bucket " + Environment.GetEnvironmentVariable("AWS_BUCKET_NAME"));
        }

        private async void OnChanged(object sender, FileSystemEventArgs e)
        {
            this.logger.LogInformation($"Created {e.Name} {e.ChangeType}");
            var markedAsCreated = await this.redis.MarkAsCreatedAsync(e.Name);

            if(new FileInfo(e.FullPath).Length == 0 && markedAsCreated)
            {
                this.logger.LogError($"File {e.Name} is empty!");
                await this.redis.MarkWithErrorAsync(e.Name);
                return;
            }

            if(!markedAsCreated)
            {
                this.logger.LogInformation($"File {e.Name} already created by another service");
                return;
            }

            try
            {
                var markedAsMoving = await this.redis.MarkAsMovingAsync(e.Name);
                await UploadFileAsync(e.Name, e.FullPath);
                await this.redis.MarkAsMovedAsync(e.Name);
            }
            catch(AmazonS3Exception ex)
            {
                this.logger.LogError("Error encountered:'{0}' when writing an object", ex.Message);
                await this.redis.MarkWithErrorAsync(e.Name);
            }
            catch(Exception ex)
            {
                this.logger.LogError("Unknown error:'{0}' when writing an object", ex.Message);
                await this.redis.MarkWithErrorAsync(e.Name);
            }

        }

        private async Task UploadFileAsync(string fileName, string filePath)
        {
            var s3Client = new AmazonS3Client(RegionEndpoint.USEast1);

            var putRequest = new PutObjectRequest
            {
                BucketName = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME"),
                Key = "test/" + fileName,
                FilePath = filePath,
                ContentType = "text/plain"
            };

            putRequest.Metadata.Add("x-amz-meta-title", fileName);
            PutObjectResponse response = await s3Client.PutObjectAsync(putRequest);

            this.logger.LogInformation($"Uploaded {fileName} with answer {response.HttpStatusCode}");

        }
    }
}
