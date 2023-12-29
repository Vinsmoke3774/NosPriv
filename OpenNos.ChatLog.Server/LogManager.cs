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

using OpenNos.Log.Shared;
using OpenNos.Core;
using OpenNos.Master.Library.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace OpenNos.Log.Server
{
    internal class LogManager
    {
        #region Members

        private static LogManager _instance;

        private readonly LogFileReader _reader;

        #endregion

        #region Instantiation

        public LogManager()
        {
            _reader = new LogFileReader();
            AuthentificatedClients = new List<long>();
            ChatLogs = new ThreadSafeGenericList<LogEntry>();
            AllChatLogs = new ThreadSafeGenericList<LogEntry>();
            PacketLogs = new ThreadSafeGenericList<PacketLogEntry>();
            AllPacketLogs = new ThreadSafeGenericList<PacketLogEntry>();
            recursiveChatFileOpen("chatlogs");
            recursivePacketFileOpen("packetlogs");
            AuthentificationServiceClient.Instance.Authenticate(ConfigurationManager.AppSettings["AuthentificationServiceAuthKey"]);
            Observable.Interval(TimeSpan.FromMinutes(1)).Subscribe(observer => SaveChatLogs());
            Observable.Interval(TimeSpan.FromMinutes(1)).Subscribe(observer => SavePacketLogs());
        }

        #endregion

        #region Properties

        public static LogManager Instance => _instance;
        
        public List<long> AuthentificatedClients { get; set; }

        public ThreadSafeGenericList<LogEntry> ChatLogs { get; set; }

        public ThreadSafeGenericList<LogEntry> AllChatLogs { get; set; }

        public ThreadSafeGenericList<PacketLogEntry> PacketLogs { get; set; }

        public ThreadSafeGenericList<PacketLogEntry> AllPacketLogs { get; set; }

        #endregion

        #region Methods

        public static void Initialize()
        {
            _instance = new LogManager();
        }

        public void SaveChatLogs()
        {
            try
            {
                LogFileWriter writer = new LogFileWriter();
                Logger.Info(Language.Instance.GetMessageFromKey("SAVE_CHATLOGS"));
                List<LogEntry> tmp = ChatLogs.GetAllItems();
                ChatLogs.Clear();
                DateTime current = DateTime.Now;

                string path = "chatlogs";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path = Path.Combine(path, current.Year.ToString());
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path = Path.Combine(path, current.Month.ToString());
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path = Path.Combine(path, current.Day.ToString());
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                writer.WriteChatLogFile(Path.Combine(path, $"{(current.Hour < 10 ? $"0{current.Hour}" : $"{current.Hour}")}.{(current.Minute < 10 ? $"0{current.Minute}" : $"{current.Minute}")}.onc"), tmp);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void SavePacketLogs()
        {
            try
            {
                LogFileWriter writer = new LogFileWriter();
                Logger.Info(Language.Instance.GetMessageFromKey("SAVE_PACKETLOGS"));
                List<PacketLogEntry> tmp = PacketLogs.GetAllItems();
                PacketLogs.Clear();
                DateTime current = DateTime.Now;

                string path = "packetlogs";
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path = Path.Combine(path, current.Year.ToString());
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path = Path.Combine(path, current.Month.ToString());
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                path = Path.Combine(path, current.Day.ToString());
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                writer.WritePacketLogFile(Path.Combine(path, $"{(current.Hour < 10 ? $"0{current.Hour}" : $"{current.Hour}")}.{(current.Minute < 10 ? $"0{current.Minute}" : $"{current.Minute}")}.onc"), tmp);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void recursiveChatFileOpen(string dir)
        {
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                foreach (string d in Directory.GetDirectories(dir))
                {
                    Parallel.ForEach(Directory.GetFiles(d).Where(s => s.EndsWith(".onc")), s =>
                    {
                        try
                        {
                            AllChatLogs.AddRange(_reader.ReadChatLogFile(s));
                        }
                        catch (Exception e)
                        {
                            Logger.LogEventError("LogFileRead", $"Something went wrong while opening Chat Log File {s}\n{e}");
                        }
                    });
                    recursiveChatFileOpen(d);
                }
            }
            catch (Exception e)
            {
                Logger.LogEventError("LogFileRead", $"Something went wrong while opening Chat Log Files. Exiting...\n{e}");
                Environment.Exit(-1);
            }
        }

        private void recursivePacketFileOpen(string dir)
        {
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                foreach (string d in Directory.GetDirectories(dir))
                {
                    Parallel.ForEach(Directory.GetFiles(d).Where(s => s.EndsWith(".onc")), s =>
                    {
                        try
                        {
                            AllPacketLogs.AddRange(_reader.ReadPacketLogFile(s));
                        }
                        catch (Exception e)
                        {
                            Logger.LogEventError("LogFileRead", $"Something went wrong while opening Packet Log File {s}\n{e}");
                        }
                    });
                    recursivePacketFileOpen(d);
                }
            }
            catch (Exception e)
            {
                Logger.LogEventError("LogFileRead", $"Something went wrong while opening Packet Log Files. Exiting...\n{e}");
                Environment.Exit(-1);
            }
        }

        #endregion

    }
}