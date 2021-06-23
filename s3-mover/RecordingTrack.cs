using System;
using System.Text.Json.Serialization;
using Newtonsoft.Json.Converters;

namespace s3_mover
{
    class RecordingTrack
    {
        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public RecordingStatus Status { get; set; }

        public DateTime ModifiedAt { get; set; }

        public string ModifiedBy { get; set; }
    }

    enum RecordingStatus
    {
        FileCreated,
        Moving,
        Moved
    }
}