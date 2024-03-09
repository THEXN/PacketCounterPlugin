using System;
using System.Collections.Generic;
using System.Configuration;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace PacketCounterPlugin
{
    [ApiVersion(2, 1)]
    public class PacketCounterPlugin : TerrariaPlugin
    {
        private readonly Dictionary<int, PacketStats> playerStats = new Dictionary<int, PacketStats>();
        public static Configuration Config;
        public PacketCounterPlugin(Main game) : base(game)
        {
            LoadConfig();
        }
        private static void LoadConfig()
        {
            Config = Configuration.Read(Configuration.FilePath);
            Config.Write(Configuration.FilePath);

        }
        private static void ReloadConfig(ReloadEventArgs args)
        {
            LoadConfig();
            args.Player?.SendSuccessMessage("[{0}] 重新加载配置完毕。", typeof(PacketCounterPlugin).Name);
        }
        public override void Initialize()
        {
            GeneralHooks.ReloadEvent += ReloadConfig;
            ServerApi.Hooks.NetSendData.Register(this, OnSendData);
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        }

        private void OnSendData(SendDataEventArgs args)
        {
            if (args.remoteClient < 0 || args.remoteClient >= 255)
                return;

            int playerIndex = args.remoteClient;

            if (!playerStats.TryGetValue(playerIndex, out PacketStats stats))
            {
                stats = new PacketStats();
                playerStats[playerIndex] = stats;
            }

            stats.AddPacket();
        }

        private void OnGameUpdate(EventArgs args)
        {
            double elapsedSeconds = Config.向控制台报告时间;

            foreach (var kvp in new Dictionary<int, PacketStats>(playerStats))
            {
                int playerIndex = kvp.Key;
                PacketStats stats = kvp.Value;

                if (stats.ElapsedSeconds >= elapsedSeconds)
                {
                    LogPacketInfo(playerIndex, stats.PacketCount);
                    stats.Reset();
                }
            }
        }

        private void LogPacketInfo(int playerIndex, int packetCount)
        {
            TSPlayer tsplayer = TShock.Players[playerIndex];
            string playerName = tsplayer?.Name ?? "Unknown";

            string logMessage = $"玩家 {playerName} 在过去 {Config.向控制台报告时间} 秒内发送了 {packetCount} 个数据包。";
            TShock.Log.ConsoleInfo(logMessage);
        }

        private class PacketStats
        {
            private int packetCount;
            private DateTime lastResetTime;

            public int PacketCount => packetCount;
            public double ElapsedSeconds => (DateTime.Now - lastResetTime).TotalSeconds;

            public void AddPacket()
            {
                packetCount++;
            }

            public void Reset()
            {
                packetCount = 0;
                lastResetTime = DateTime.Now;
            }
        }
    }
}
