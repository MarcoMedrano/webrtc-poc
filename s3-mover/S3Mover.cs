using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.FileProviders;

/// <summary>
/// This program will upload to S3 the file created in the specified path in arguments.
/// Environemnt variables required:
/// AWS_ACCESS_KEY_ID
/// AWS_SECRET_KEY
/// AWS_BUCKET_NAME
/// 
/// This application can be run as AWS_BUCKET_NAME=<AWS_BUCKET_NAME> AWS_ACCESS_KEY_ID=<AWS_ACCESS_KEY_ID> AWS_SECRET_KEY=<AWS_SECRET_KEY> ./s3-mover /path-to-watch
/// </summary>
namespace s3_mover
{
    class S3Mover
    {
        private const string FILTER = "*.webm";
        private string path = string.Empty;

        public void Run(string folder)
        {
            Console.WriteLine("S3 Mover 4");
            path = !string.IsNullOrEmpty(folder) ? folder : Environment.CurrentDirectory;

            // var watcher = new FileSystemWatcher();
            // watcher.Path = path;
            // watcher.NotifyFilter = NotifyFilters.LastWrite;
            // watcher.Filter = FILTER;
            // watcher.Changed += OnChanged;
            // watcher.EnableRaisingEvents = true;

            // FileSystemWatcher not working in Docker image other than the MS one.
            // so had to implement soemthing more manual
            var watcher = new FileWatcher();
            watcher.Path = path;
            watcher.Filter = FILTER;
            watcher.Changed += OnChanged;

            Console.WriteLine("Watching folder " + watcher.Path);
            Console.WriteLine("Bucket " + Environment.GetEnvironmentVariable("AWS_BUCKET_NAME"));
        }

        private async void OnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"Changed {e.Name} {e.ChangeType}");
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

                Console.WriteLine("Uploaded with answer " + response.HttpStatusCode);
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered:'{0}' when writing an object", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown error:'{0}' when writing an object", e.Message);
            }
        }
    }
}
