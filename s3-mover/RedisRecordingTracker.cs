using System;
using StackExchange.Redis;

namespace s3_mover
{
    class RedisRecordingTracker : IDisposable
    {
        private readonly string source;
        private readonly ConnectionMultiplexer redis;

        RedisRecordingTracker(string source)
        {
            this.source = source;

            this.redis = ConnectionMultiplexer.Connect("localhost");
        }

        public void Dispose()
        {
            this.redis.Dispose();
        }

        async void MarkAsCreatedAsync(string fileName)
        {
            var track = new RecordingTrack
            {
                Name = fileName,
                ModifiedAt = DateTime.UtcNow,
                ModifiedBy = source,
                Status = RecordingStatus.FileCreated
            };
        }
    }
}