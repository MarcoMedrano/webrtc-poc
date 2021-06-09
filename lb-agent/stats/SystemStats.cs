namespace lb_agent
{
    static class SystemStats 
    {
        public static Memory Memory = default(Memory); 
    }

    public class Memory
    {
        public int Total;
        public int Used;
    }
}