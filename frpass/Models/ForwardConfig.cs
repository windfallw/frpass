namespace frpass.Models
{
    public class ForwardConfig
    {
        public string config_name { get; set; }

        public string type { get; set; }

        public string local_ip { get; set; }

        public int local_port { get; set; }

        public int remote_port { get; set; }
    }
}