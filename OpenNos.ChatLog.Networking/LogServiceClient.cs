/*
 * This file is part of the OpenNos Emulator Project. See AUTHORS file for Copyright information
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 */

using System;
using System.Configuration;
using OpenNos.Log.Shared;
using Hik.Communication.ScsServices.Client;
using Hik.Communication.Scs.Communication;
using Hik.Communication.Scs.Communication.EndPoints.Tcp;
using OpenNos.Core;
using System.Collections.Generic;
using OpenNos.Data;
using OpenNos.Domain;

namespace OpenNos.Log.Networking
{
    public class LogServiceClient : ILogService
    {
        #region Members

        private static LogServiceClient _instance;

        private readonly IScsServiceClient<ILogService> _client;

        #endregion

        #region Instantiation

        public LogServiceClient()
        {
            string ip = ConfigurationManager.AppSettings["LogIP"];
            int port = Convert.ToInt32(ConfigurationManager.AppSettings["LogPort"]);
            _client = ScsServiceClientBuilder.CreateClient<ILogService>(new ScsTcpEndPoint(ip, port));
            System.Threading.Thread.Sleep(1000);
            while (_client.CommunicationState != CommunicationStates.Connected)
            {
                try
                {
                    _client.Connect();
                }
                catch (Exception)
                {
                    Logger.Error(Language.Instance.GetMessageFromKey("RETRY_CONNECTION"), memberName: nameof(LogServiceClient));
                    System.Threading.Thread.Sleep(1000);
                }
            }
        }

        #endregion

        #region Properties

        public static LogServiceClient Instance => _instance ?? (_instance = new LogServiceClient());

        public CommunicationStates CommunicationState => _client.CommunicationState;

        #endregion

        #region Methods

        public void LogChatMessage(LogEntry logEntry) => _client.ServiceProxy.LogChatMessage(logEntry);

        public void LogPacket(PacketLogEntry logEntry) => _client.ServiceProxy.LogPacket(logEntry);

        public bool AuthenticateAdmin(string user, string passHash) => _client.ServiceProxy.AuthenticateAdmin(user, passHash);

        public AccountDTO GetAccount(string user, string passHash) => _client.ServiceProxy.GetAccount(user, passHash);

        public List<LogEntry> GetChatLogEntries(string[] sender, string[] senderid, string[] receiver, string[] receiverid, string[] message, DateTime? start, DateTime? end, LogType? logType) => _client.ServiceProxy.GetChatLogEntries(sender, senderid, receiver, receiverid, message, start, end, logType);

        public List<PacketLogEntry> GetPacketLogEntries(string[] sender, string[] senderid, string[] receiver, string[] receiverid, string[] message, DateTime? start, DateTime? end, LogType? logType, AuthorityType authority) => _client.ServiceProxy.GetPacketLogEntries(sender, senderid, receiver, receiverid, message, start, end, logType, authority);

        public bool Authenticate(string authKey) => _client.ServiceProxy.Authenticate(authKey);

        #endregion
    }
}