using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenNos.Log.Shared
{
    public class LogFileReader
    {
        public List<LogEntry> ReadChatLogFile(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                using (BinaryReader br = new BinaryReader(stream))
                {
                    string header = $"{br.ReadChar()}{br.ReadChar()}{br.ReadChar()}";
                    byte version = br.ReadByte();
                    if (header == "ONC")
                    {
                        switch (version)
                        {
                            case 1:
                                return ReadChatLogVersion(br);
                            default:
                                throw new InvalidDataException("File Version invalid!");
                        }
                    }
                    else
                    {
                        throw new InvalidDataException("File Header invalid!");
                    }
                }
            }
        }

        public List<PacketLogEntry> ReadPacketLogFile(string path)
        {
            using (FileStream stream = File.OpenRead(path))
            {
                using (BinaryReader br = new BinaryReader(stream))
                {
                    string header = $"{br.ReadChar()}{br.ReadChar()}{br.ReadChar()}";
                    byte version = br.ReadByte();
                    if (header == "ONC")
                    {
                        switch (version)
                        {
                            case 1:
                                return ReadPacketLogVersion(br);
                            default:
                                throw new InvalidDataException("File Version invalid!");
                        }
                    }
                    else
                    {
                        throw new InvalidDataException("File Header invalid!");
                    }
                }
            }
        }

        private List<LogEntry> ReadChatLogVersion(BinaryReader reader)
        {
            List<LogEntry> result = new List<LogEntry>();
            int count = reader.ReadInt32();
            while (count != 0)
            {
                DateTime timestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(reader.ReadDouble());
                LogType chatLogType = (LogType)reader.ReadByte();
                string sender = reader.ReadString();
                long senderId = reader.ReadInt64();
                string receiver = reader.ReadString();
                long receiverId = reader.ReadInt64();
                string message = reader.ReadString();
                result.Add(new LogEntry()
                {
                    Timestamp = timestamp,
                    MessageType = chatLogType,
                    Sender = sender,
                    SenderId = senderId,
                    Receiver = receiver,
                    ReceiverId = receiverId,
                    Message = message
                });
                count--;
            }
            return result;
        }

        private List<PacketLogEntry> ReadPacketLogVersion(BinaryReader reader)
        {
            List<PacketLogEntry> result = new List<PacketLogEntry>();
            int count = reader.ReadInt32();
            while (count != 0)
            {
                DateTime timestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(reader.ReadDouble());
                LogType logType = (LogType)reader.ReadByte();
                string sender = reader.ReadString();
                long senderId = reader.ReadInt64();
                string receiver = reader.ReadString();
                long receiverId = reader.ReadInt64();
                string packet = reader.ReadString();
                result.Add(new PacketLogEntry()
                {
                    Timestamp = timestamp,
                    PacketType = logType,
                    Sender = sender,
                    SenderId = senderId,
                    Receiver = receiver,
                    ReceiverId = receiverId,
                    Packet = packet
                });
                count--;
            }
            return result;
        }
    }
}
