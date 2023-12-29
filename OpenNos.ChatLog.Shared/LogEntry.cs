using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNos.Log.Shared
{
    [Serializable]
    public class LogEntry
    {
        public string Sender { get; set; }

        public long? SenderId { get; set; }

        public string Receiver { get; set; }

        public long? ReceiverId { get; set; }

        public LogType MessageType { get; set; }

        public string Message { get; set; }

        public DateTime Timestamp { get; set; }

        public override string ToString() => $"[{Timestamp}]<{MessageType}> {Sender}({SenderId})->{Receiver}({ReceiverId}) > {Message}";
    }
}
