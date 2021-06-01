namespace signaling
{

    public class Turn
    {
        public string ip { get; set; }
        public int port { get; set; }
        public string username { get; set; }
        public string credential { get; set; }
    }

    public class Settings
    {
        public Turn Turn { get; set; }
    }
}