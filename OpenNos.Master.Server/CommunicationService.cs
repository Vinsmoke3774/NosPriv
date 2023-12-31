﻿/*
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

using OpenNos.Core;
using OpenNos.DAL;
using OpenNos.Data;
using OpenNos.Domain;
using OpenNos.GameObject;
using OpenNos.Master.Library.Data;
using OpenNos.Master.Library.Interface;
using OpenNos.SCS.Communication.Scs.Communication.EndPoints.Tcp;
using OpenNos.SCS.Communication.ScsServices.Service;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reactive.Linq;

namespace OpenNos.Master.Server
{
    internal class CommunicationService : ScsService, ICommunicationService
    {
        #region Methods

        public bool Authenticate(string authKey)
        {
            if (string.IsNullOrWhiteSpace(authKey))
            {
                return false;
            }

            return false;
        }

        public void Cleanup()
        {


            MSManager.Instance.ConnectedAccounts.Clear();
            MSManager.Instance.WorldServers.Clear();
        }

        public void CleanupOutdatedSession()
        {


            AccountConnection[] tmp = new AccountConnection[MSManager.Instance.ConnectedAccounts.Count + 20];
            lock (MSManager.Instance.ConnectedAccounts)
            {
                MSManager.Instance.ConnectedAccounts.CopyTo(tmp);
            }
            foreach (AccountConnection account in tmp.Where(a => a?.LastPulse.AddMinutes(5) <= DateTime.Now))
            {
                KickSession(account.AccountId, null);
            }
        }

        public bool ConnectAccount(Guid worldId, long accountId, int sessionId)
        {


            AccountConnection account = MSManager.Instance.ConnectedAccounts.Find(a => a.AccountId.Equals(accountId) && a.SessionId.Equals(sessionId));
            if (account != null)
            {
                account.ConnectedWorld = MSManager.Instance.WorldServers.Find(w => w.Id.Equals(worldId));
            }
            return account.ConnectedWorld != null;
        }

        public bool ConnectAccountCrossServer(Guid worldId, long accountId, int sessionId)
        {

            AccountConnection account = MSManager.Instance.ConnectedAccounts.Where(a => a.AccountId.Equals(accountId) && a.SessionId.Equals(sessionId)).FirstOrDefault();
            if (account != null)
            {
                account.CanLoginCrossServer = false;
                account.OriginWorld = account.ConnectedWorld;
                account.ConnectedWorld = MSManager.Instance.WorldServers.Find(s => s.Id.Equals(worldId));
                if (account.ConnectedWorld != null)
                {
                    return true;
                }
            }
            return false;
        }

        public bool ConnectCharacter(Guid worldId, long characterId)
        {


            //Multiple WorldGroups not yet supported by DAOFactory
            long accountId = DAOFactory.CharacterDAO.LoadById(characterId)?.AccountId ?? 0;

            AccountConnection account = MSManager.Instance.ConnectedAccounts.Find(a => a.AccountId.Equals(accountId) && a.ConnectedWorld.Id.Equals(worldId));
            if (account != null)
            {
                account.CharacterId = characterId;
                foreach (WorldServer world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(account.ConnectedWorld.WorldGroup)))
                {
                    world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().CharacterConnected(characterId);
                }
                return true;
            }
            return false;
        }

        public void DisconnectAccount(long accountId)
        {

            if (MSManager.Instance.ConnectedAccounts.Any(s => s.AccountId.Equals(accountId) && s.CanLoginCrossServer))
            {
            }
            else
            {
                MSManager.Instance.ConnectedAccounts.RemoveAll(c => c.AccountId.Equals(accountId));
            }
        }

        public void DisconnectCharacter(Guid worldId, long characterId)
        {


            foreach (AccountConnection account in MSManager.Instance.ConnectedAccounts.Where(c => c.CharacterId.Equals(characterId) && c.ConnectedWorld.Id.Equals(worldId)))
            {
                foreach (WorldServer world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(account.ConnectedWorld.WorldGroup)))
                {
                    world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().CharacterDisconnected(characterId);
                }
                if (!account.CanLoginCrossServer)
                {
                    account.CharacterId = 0;
                    account.ConnectedWorld = null;
                }
            }
        }

        public int? GetChannelIdByWorldId(Guid worldId) => MSManager.Instance.WorldServers.Find(w => w.Id == worldId)?.ChannelId;

        public long[][] GetOnlineCharacters()
        {


            List<AccountConnection> connections = MSManager.Instance.ConnectedAccounts.Where(s => s.CharacterId != 0);

            long[][] result = new long[connections.Count][];

            int i = 0;
            foreach (AccountConnection acc in connections)
            {
                result[i] = new long[3];
                result[i][0] = acc.CharacterId;
                result[i][1] = acc.ConnectedWorld?.ChannelId ?? 0;
                result[i][2] = acc.SessionId;
                i++;
            }
            return result;
        }

        public bool IsAccountConnected(long accountId)
        {


            return MSManager.Instance.ConnectedAccounts.Any(c => c.AccountId == accountId && c.ConnectedWorld != null && c.LastPulse.AddSeconds(90) >= DateTime.Now);
        }

        public bool IsAct4Online(string worldGroup) => MSManager.Instance.WorldServers.Any(w => w.WorldGroup.Equals(worldGroup)
            && w.Endpoint.IpAddress == MSManager.Instance.ConfigurationObject.Act4IP && w.Endpoint.TcpPort == MSManager.Instance.ConfigurationObject.Act4Port);

        public bool IsCharacterConnected(string worldGroup, long characterId)
        {


            return MSManager.Instance.ConnectedAccounts.Any(c => c.ConnectedWorld != null && c.ConnectedWorld.WorldGroup == worldGroup && c.CharacterId == characterId);
        }

        public bool IsCrossServerLoginPermitted(long accountId, int sessionId)
        {


            return MSManager.Instance.ConnectedAccounts.Any(s => s.AccountId.Equals(accountId) && s.SessionId.Equals(sessionId) && s.CanLoginCrossServer);
        }

        public bool IsLoginPermitted(long accountId, int sessionId)
        {


            return MSManager.Instance.ConnectedAccounts.Any(s => s.AccountId.Equals(accountId) && s.SessionId.Equals(sessionId) && s.ConnectedWorld == null);
        }

        public void KickSession(long? accountId, int? sessionId)
        {


            foreach (WorldServer world in MSManager.Instance.WorldServers)
            {
                world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().KickSession(accountId, sessionId);
            }
            if (accountId.HasValue)
            {
                MSManager.Instance.ConnectedAccounts.RemoveAll(s => s.AccountId.Equals(accountId.Value));
            }
            else if (sessionId.HasValue)
            {
                MSManager.Instance.ConnectedAccounts.RemoveAll(s => s.SessionId.Equals(sessionId.Value));
            }
        }

        public void PulseAccount(long accountId)
        {

            AccountConnection account = MSManager.Instance.ConnectedAccounts.Find(a => a.AccountId.Equals(accountId));
            if (account != null)
            {
                account.LastPulse = DateTime.Now;
            }
        }

        public void RefreshPenalty(int penaltyId)
        {


            foreach (WorldServer world in MSManager.Instance.WorldServers)
            {
                world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().UpdatePenaltyLog(penaltyId);
            }
            foreach (IScsServiceClient login in MSManager.Instance.LoginServers)
            {
                login.GetClientProxy<ICommunicationClient>().UpdatePenaltyLog(penaltyId);
            }
        }

        public void RegisterAccountLogin(long accountId, int sessionId, string ipAddress)
        {
            MSManager.Instance.ConnectedAccounts.RemoveAll(a => a.AccountId.Equals(accountId));
            MSManager.Instance.ConnectedAccounts.Add(new AccountConnection(accountId, sessionId, ipAddress));
        }

        public void RegisterCrossServerAccountLogin(long accountId, int sessionId)
        {

            AccountConnection account = MSManager.Instance.ConnectedAccounts.Where(a => a.AccountId.Equals(accountId) && a.SessionId.Equals(sessionId)).FirstOrDefault();

            if (account != null)
            {
                account.CanLoginCrossServer = true;
            }
        }

        public int? RegisterWorldServer(SerializableWorldServer worldServer)
        {

            WorldServer ws = new WorldServer(worldServer.Id, new ScsTcpEndPoint(worldServer.EndPointIP, worldServer.EndPointPort), worldServer.AccountLimit, worldServer.WorldGroup)
            {
                CommunicationServiceClient = CurrentClient,
                ChannelId = Enumerable.Range(1, 30).Except(MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(worldServer.WorldGroup)).OrderBy(w => w.ChannelId).Select(w => w.ChannelId)).First()
            };
            if (worldServer.EndPointPort == MSManager.Instance.ConfigurationObject.Act4Port)
            {
                ws.ChannelId = 51;
            }
            MSManager.Instance.WorldServers.Add(ws);
            return ws.ChannelId;
        }

        public void Restart(string worldGroup, int time = 5)
        {


            if (worldGroup == "*")
            {
                foreach (WorldServer world in MSManager.Instance.WorldServers)
                {
                    world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().Restart(time);
                }
            }
            else
            {
                foreach (WorldServer world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(worldGroup)))
                {
                    world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().Restart(time);
                }
            }
        }

        public long[][] RetrieveOnlineCharacters(long characterId)
        {


            List<AccountConnection> connections = MSManager.Instance.ConnectedAccounts.Where(s => s.IpAddress == MSManager.Instance.ConnectedAccounts.Find(f => f.CharacterId == characterId)?.IpAddress && s.CharacterId != 0);

            long[][] result = new long[connections.Count][];

            int i = 0;
            foreach (AccountConnection acc in connections)
            {
                result[i] = new long[2];
                result[i][0] = acc.CharacterId;
                result[i][1] = acc.ConnectedWorld?.ChannelId ?? 0;
                i++;
            }
            return result;
        }

        public string RetrieveOriginWorld(long accountId)
        {


            AccountConnection account = MSManager.Instance.ConnectedAccounts.Find(s => s.AccountId.Equals(accountId));
            if (account?.OriginWorld != null)
            {
                return $"{account.OriginWorld.Endpoint.IpAddress}:{account.OriginWorld.Endpoint.TcpPort}";
            }
            return null;
        }

        public string RetrieveRegisteredWorldServers(string username, int sessionId, bool ignoreUserName)
        {

            string lastGroup = "";
            byte worldCount = 0;
            string channelPacket = "NsTeST" + (ignoreUserName ? "" : " " + username) + $" {sessionId} ";

            foreach (WorldServer world in MSManager.Instance.WorldServers.OrderBy(w => w.WorldGroup))
            {
                if (lastGroup != world.WorldGroup)
                {
                    worldCount++;
                }
                lastGroup = world.WorldGroup;

                int currentlyConnectedAccounts = MSManager.Instance.ConnectedAccounts.CountLinq(a => a.ConnectedWorld?.ChannelId == world.ChannelId);
                int channelcolor = (int)Math.Round(((double)currentlyConnectedAccounts / world.AccountLimit) * 20) + 1;

                if (world.ChannelId == 51)
                {
                    continue;
                }

                channelPacket += $"{world.Endpoint.IpAddress}:{world.Endpoint.TcpPort}:{channelcolor}:{worldCount}.{world.ChannelId}.{world.WorldGroup} ";
            }
            return channelPacket;
        }

        public IEnumerable<string> RetrieveServerStatistics()
        {


            List<string> result = new List<string>();

            try
            {
                List<string> groups = new List<string>();
                foreach (string s in MSManager.Instance.WorldServers.Select(s => s.WorldGroup))
                {
                    if (!groups.Contains(s))
                    {
                        groups.Add(s);
                    }
                }
                int totalsessions = 0;
                foreach (string message in groups)
                {
                    result.Add($"==={message}===");
                    int groupsessions = 0;
                    foreach (WorldServer world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(message)))
                    {
                        int sessions = MSManager.Instance.ConnectedAccounts.CountLinq(a => a.ConnectedWorld?.Id.Equals(world.Id) == true);
                        result.Add($"Channel {world.ChannelId}: {sessions} Sessions");
                        groupsessions += sessions;
                    }
                    result.Add($"Group Total: {groupsessions} Sessions");
                    totalsessions += groupsessions;
                }
                result.Add($"Environment Total: {totalsessions} Sessions");
            }
            catch (Exception ex)
            {
                Logger.LogEventError("RETRIEVE_EXCEPTION", "Error while retreiving server Statistics:", ex);
            }

            return result;
        }

        public void RunGlobalEvent(EventType eventType)
        {

            foreach (WorldServer world in MSManager.Instance.WorldServers)
            {
                world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().RunGlobalEvent(eventType);
            }
        }

        public int? SendMessageToCharacter(SCSCharacterMessage message)
        {


            WorldServer sourceWorld = MSManager.Instance.WorldServers.Find(s => s.Id.Equals(message.SourceWorldId));
            if (message == null || message.Message == null || sourceWorld == null)
            {
                return null;
            }

            switch (message.Type)
            {
                case MessageType.Family:
                case MessageType.FamilyChat:
                case MessageType.Broadcast:
                    foreach (WorldServer world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(sourceWorld.WorldGroup)))
                    {
                        world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().SendMessageToCharacter(message);
                    }
                    return -1;

                case MessageType.PrivateChat:
                    if (message.DestinationCharacterId.HasValue)
                    {
                        AccountConnection receiverAccount = MSManager.Instance.ConnectedAccounts.Find(a => a.CharacterId.Equals(message.DestinationCharacterId.Value));
                        if (receiverAccount?.ConnectedWorld != null)
                        {
                            if (sourceWorld.ChannelId == 51 && receiverAccount.ConnectedWorld.ChannelId == 51
                                && DAOFactory.CharacterDAO.LoadById(message.SourceCharacterId).Faction
                                != DAOFactory.CharacterDAO.LoadById((long)message.DestinationCharacterId).Faction)
                            {
                                AccountConnection SenderAccount = MSManager.Instance.ConnectedAccounts.Find(a => a.CharacterId.Equals(message.SourceCharacterId));
                                message.Message = $"talk {message.DestinationCharacterId} " + Language.Instance.GetMessageFromKey("CANT_TALK_OPPOSITE_FACTION");
                                message.DestinationCharacterId = message.SourceCharacterId;
                                SenderAccount.ConnectedWorld.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().SendMessageToCharacter(message);
                                return -1;
                            }
                            else
                            {
                                receiverAccount.ConnectedWorld.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().SendMessageToCharacter(message);
                                return receiverAccount.ConnectedWorld.ChannelId;
                            }
                        }
                    }
                    break;
                case MessageType.Whisper:
                    if (message.DestinationCharacterId.HasValue)
                    {
                        AccountConnection receiverAccount = MSManager.Instance.ConnectedAccounts.Find(a => a.CharacterId.Equals(message.DestinationCharacterId.Value));
                        if (receiverAccount?.ConnectedWorld != null)
                        {
                            if (sourceWorld.ChannelId == 51 && receiverAccount.ConnectedWorld.ChannelId == 51
                                && DAOFactory.CharacterDAO.LoadById(message.SourceCharacterId).Faction
                                != DAOFactory.CharacterDAO.LoadById((long)message.DestinationCharacterId).Faction)
                            {
                                AccountConnection SenderAccount = MSManager.Instance.ConnectedAccounts.Find(a => a.CharacterId.Equals(message.SourceCharacterId));
                                message.Message = $"say 1 {message.SourceCharacterId} 11 {Language.Instance.GetMessageFromKey("CANT_TALK_OPPOSITE_FACTION")}";
                                message.DestinationCharacterId = message.SourceCharacterId;
                                message.Type = MessageType.Other;
                                receiverAccount.ConnectedWorld.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().SendMessageToCharacter(message);
                                return -1;
                            }
                            else
                            {
                                receiverAccount.ConnectedWorld.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().SendMessageToCharacter(message);
                                return receiverAccount.ConnectedWorld.ChannelId;
                            }
                        }
                    }
                    break;
                case MessageType.WhisperSupport:
                case MessageType.WhisperGM:
                    if (message.DestinationCharacterId.HasValue)
                    {
                        AccountConnection account = MSManager.Instance.ConnectedAccounts.Find(a => a.CharacterId.Equals(message.DestinationCharacterId.Value));
                        if (account?.ConnectedWorld != null)
                        {
                            account.ConnectedWorld.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().SendMessageToCharacter(message);
                            return account.ConnectedWorld.ChannelId;
                        }
                    }
                    break;

                case MessageType.Shout:
                    foreach (WorldServer world in MSManager.Instance.WorldServers)
                    {
                        world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().SendMessageToCharacter(message);
                    }
                    return -1;

                case MessageType.Other:
                    AccountConnection receiverAcc = MSManager.Instance.ConnectedAccounts.Find(a => a.CharacterId.Equals(message.DestinationCharacterId.Value));
                    if (receiverAcc?.ConnectedWorld != null)
                    {
                        receiverAcc.ConnectedWorld.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().SendMessageToCharacter(message);
                        return receiverAcc.ConnectedWorld.ChannelId;
                    }
                    break;
            }
            return null;
        }

        public void Shutdown(string worldGroup)
        {


            if (worldGroup == "*")
            {
                foreach (WorldServer world in MSManager.Instance.WorldServers)
                {
                    world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().Shutdown();
                }
            }
            else
            {
                foreach (WorldServer world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(worldGroup)))
                {
                    world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().Shutdown();
                }
            }
        }

        public void UnregisterWorldServer(Guid worldId)
        {


            MSManager.Instance.ConnectedAccounts.RemoveAll(a => a?.ConnectedWorld?.Id.Equals(worldId) == true);
            MSManager.Instance.WorldServers.RemoveAll(w => w.Id.Equals(worldId));
        }

        public void UpdateBazaar(string worldGroup, long bazaarItemId)
        {


            foreach (WorldServer world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(worldGroup)))
            {
                world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().UpdateBazaar(bazaarItemId);
            }
        }

        public void UpdateFamily(string worldGroup, long familyId, bool changeFaction)
        {


            foreach (WorldServer world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(worldGroup)))
            {
                world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().UpdateFamily(familyId, changeFaction);
            }
        }

        public void UpdateRelation(string worldGroup, long relationId)
        {


            foreach (WorldServer world in MSManager.Instance.WorldServers.Where(w => w.WorldGroup.Equals(worldGroup)))
            {
                world.CommunicationServiceClient.GetClientProxy<ICommunicationClient>().UpdateRelation(relationId);
            }
        }

        #endregion
    }
}