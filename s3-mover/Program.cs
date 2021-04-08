using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Amazon;
using Amazon.S3;
using Amazon.S3.Model;

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
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("S3 Mover ");

            var watcher = new FileSystemWatcher();
            watcher.Path = args.Length > 0 ? args[0] : Environment.CurrentDirectory;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.Filter = "*.webm";
            watcher.Changed += OnChanged;
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Watching folder " + watcher.Path);
            Console.ReadKey();
        }

        private static async void OnChanged(object sender, FileSystemEventArgs e)
        {
            Console.WriteLine($"{e.Name} {e.ChangeType}");
            await UploadFileAsync(e.Name, e.FullPath);
        }

        private static async Task UploadFileAsync(string fileName, string filePath)
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
