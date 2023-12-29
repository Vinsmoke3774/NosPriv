using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNos.Log.Shared
{
    [Serializable]
    public class PacketLogEntry
    {
        public string Sender { get; set; }

        public long? SenderId { get; set; }

        public string Receiver { get; set; }

        public long? ReceiverId { get; set; }

        public LogType PacketType { get; set; }

        public string Packet { get; set; }

        public DateTime Timestamp { get; set; }
        
        public override string ToString() => $"[{Timestamp}]<{PacketType}> {Sender}({SenderId})->{Receiver}({ReceiverId}) > {Packet}";
    }
}
