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

        public S3Mover(ILogger<Worker> logger)
        {
            this.logger = logger;
        }

        public void Run(string folder)
        {
            this.logger.LogInformation("S3 Mover 10");
            path = !string.IsNullOrEmpty(folder) ? folder : Environment.CurrentDirectory;

            // var watcher2 = new FileSystemWatcher();
            // watcher2.Path = path;
            // watcher2.NotifyFilter = NotifyFilters.LastWrite;
            // watcher2.Filter = FILTER;
            // watcher2.Changed += (object sender, FileSystemEventArgs e) => { this.logger.LogInformation($"FSW {e.Name} {e.ChangeType}"); };
            // watcher2.EnableRaisingEvents = true;

            // FileSystemWatcher not working in Docker image other than the MS one.
            // so had to implement something more manual
            var watcher = new FileWatcher(this.logger);
            watcher.Path = path;
            watcher.Filter = FILTER;
            watcher.Changed += OnChanged;

            this.logger.LogInformation("Watching folder " + watcher.Path);
            this.logger.LogInformation("Bucket " + Environment.GetEnvironmentVariable("AWS_BUCKET_NAME"));
        }

        private async void OnChanged(object sender, FileSystemEventArgs e)
        {
            this.logger.LogInformation($"Changed {e.Name} {e.ChangeType}");
            await UploadFileAsync(e.Name, e.FullPath);
        }

        private async Task UploadFileAsync(string fileName, string filePath)
        {
            var s3Client = new AmazonS3Client(RegionEndpoint.USEast1);

            try
            {
                var putRequest = new PutObjectRequest
                {
                    BucketName = Environment.GetEnvironmentVariable("AWS_BUCKET_NAME"),
                    Key = "test/" + fileName,
                    FilePath = filePath,
                    ContentType = "text/plain"
                };

                putRequest.Metadata.Add("x-amz-meta-title", fileName);
                PutObjectResponse response = await s3Client.PutObjectAsync(putRequest);

                this.logger.LogInformation("Uploaded with answer " + response.HttpStatusCode);
            }
            catch(AmazonS3Exception e)
            {
                this.logger.LogError("Error encountered:'{0}' when writing an object", e.Message);
            }
            catch(Exception e)
            {
                this.logger.LogError("Unknown error:'{0}' when writing an object", e.Message);
            }
        }
    }
}
