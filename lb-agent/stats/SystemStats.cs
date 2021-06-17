namespace lb_agent
{
    static class SystemStats
    {
        public static Memory Memory = new Memory();

        public static bool MaintenanceMode { get; internal set; }
    }

    public class Memory
    {
        public int Total { get; set; } = 1024;
        public int Available { get; set; } = 1024;
    }
}