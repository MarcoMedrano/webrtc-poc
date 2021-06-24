using System;
namespace s3_mover
{
    class RecordingTrack
    {
        public string Name { get; set; }

        public RecordingStatus Status { get; set; }

        public DateTime TrackedAt { get; set; }

        public string TrackedBy { get; set; }
    }

    enum RecordingStatus
    {
        FileCreated,
        Moving,
        Moved,
        WithError
    }
}