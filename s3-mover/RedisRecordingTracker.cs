using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using StackExchange.Redis;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Converters;

namespace s3_mover
{
    class RedisRecordingTracker : IDisposable
    {
        private readonly string source;
        private readonly ILogger<Worker> logger;
        private readonly ConnectionMultiplexer redis;
        private readonly IDatabase db;

        internal RedisRecordingTracker(string redisConnectionString, string source, ILogger<Worker> logger)
        {
            this.source = source;
            this.logger = logger;

            this.redis = ConnectionMultiplexer.Connect(redisConnectionString);
            this.db = redis.GetDatabase();
        }

        public void Dispose()
        {
            this.redis.Dispose();
        }

        internal async Task<bool> MarkAsCreatedAsync(string fileName)
        {
            var currentJson = await db.StringGetAsync(fileName);

            if(!currentJson.IsNullOrEmpty)
            {

                this.logger.LogInformation($"{fileName} Already had value {currentJson}");
                var current = JsonConvert.DeserializeObject<RecordingTrack>(currentJson, new StringEnumConverter());

                if(current.Status != RecordingStatus.WithError)
                    return false;
            }

            return await this.MarkAsAsync(fileName, RecordingStatus.FileCreated);
        }

        internal async Task<bool> MarkAsMovingAsync(string fileName)
        {
            return await this.MarkAsAsync(fileName, RecordingStatus.Moving);
        }

        internal async Task<bool> MarkAsMovedAsync(string fileName)
        {
            return await this.MarkAsAsync(fileName, RecordingStatus.Moved);
        }

        internal async Task<bool> MarkWithErrorAsync(string fileName)
        {
            return await this.MarkAsAsync(fileName, RecordingStatus.WithError);
        }

        private async Task<bool> MarkAsAsync(string fileName, RecordingStatus status)
        {
            this.logger.LogInformation($"Marking {fileName} as {status}");

            var track = new RecordingTrack
            {
                Name = fileName,
                TrackedAt = DateTime.UtcNow,
                TrackedBy = source,
                Status = status
            };

            var json = JsonConvert.SerializeObject(track, new StringEnumConverter());

            return await db.StringSetAsync(fileName, json);
        }
    }
}