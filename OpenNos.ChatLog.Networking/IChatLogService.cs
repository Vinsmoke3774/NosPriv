using Hik.Communication.ScsServices.Service;
using OpenNos.Log.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenNos.Data;
using OpenNos.Domain;

namespace OpenNos.Log.Networking
{
    [ScsService(Version = "1.1.0.0")]
    public interface ILogService
    {
        /// <summary>
        /// Authenticates a Client to the Service
        /// </summary>
        /// <param name="authKey">The private Authentication key</param>
        /// <returns>true if successful, else false</returns>
        bool Authenticate(string authKey);

        /// <summary>
        /// Authenticates a Client to the Service
        /// </summary>
        /// <param name="user"></param>
        /// <param name="passHash"></param>
        /// <returns></returns>
        bool AuthenticateAdmin(string user, string passHash);

        /// <summary>
        /// Log Chat Message to Chat Log Server
        /// </summary>
        /// <param name="logEntry"></param>
        void LogChatMessage(LogEntry logEntry);

        /// <summary>
        /// Log Chat Message to Chat Log Server
        /// </summary>
        /// <param name="logEntry"></param>
        void LogPacket(PacketLogEntry logEntry);

        /// <summary>
        /// Receive Log Entries from Chat Log Server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="senderid"></param>
        /// <param name="receiver"></param>
        /// <param name="receiverid"></param>
        /// <param name="message"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="logType"></param>
        /// <returns></returns>
        List<LogEntry> GetChatLogEntries(string[] sender, string[] senderid, string[] receiver, string[] receiverid, string[] message, DateTime? start, DateTime? end, LogType? logType);

        /// <summary>
        /// Receive Log Entries from Chat Log Server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="senderid"></param>
        /// <param name="receiver"></param>
        /// <param name="receiverid"></param>
        /// <param name="packet"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="logType"></param>
        /// <param name="authority"></param>
        /// <returns></returns>
        List<PacketLogEntry> GetPacketLogEntries(string[] sender, string[] senderid, string[] receiver, string[] receiverid, string[] message, DateTime? start, DateTime? end, LogType? logType, AuthorityType authority);

        /// <summary>
        /// Get the Authenticated Account
        /// </summary>
        /// <param name="user"></param>
        /// <param name="passHash"></param>
        /// <returns></returns>
        AccountDTO GetAccount(string user, string passHash);
    }
}
