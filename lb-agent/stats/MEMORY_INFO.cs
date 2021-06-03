namespace lb_agent
{


    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct MEMORY_INFO
    {
        public uint dwLength; // current structure size
        public uint dwMemoryLoad; // current memory usage
        public long ullTotalPhys; // Total physical memory size
        public long ullAvailPhys; // Available physical memory size
        public long ullTotalPageFile; // Total swap file size
        public long ullAvailPageFile; // Total swap file size
        public long ullTotalVirtual; // Total virtual memory size
        public long ullAvailVirtual; // Available virtual memory size
        public long ullAvailExtendedVirtual; // Keep this value always 0
    }
}