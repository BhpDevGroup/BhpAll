﻿using Microsoft.Extensions.Configuration;
using Bhp.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bhp
{
    public class ProtocolSettings
    {
        public uint Magic { get; }
        public byte AddressVersion { get; }
        public string[] StandbyValidators { get; }
        public string[] SeedList { get; }
        public IReadOnlyDictionary<TransactionType, Fixed8> SystemFee { get; }
        public Fixed8 LowPriorityThreshold { get; }
        public uint SecondsPerBlock { get; }

        public static ProtocolSettings Default { get; }

        static ProtocolSettings()
        {
            IConfigurationSection section = new ConfigurationBuilder().AddJsonFile("protocol.json").Build().GetSection("ProtocolConfiguration");
            Default = new ProtocolSettings(section);
        }

        private ProtocolSettings(IConfigurationSection section)
        {
            Magic = uint.Parse(section.GetSection("Magic").Value);
            AddressVersion = byte.Parse(section.GetSection("AddressVersion").Value);
            StandbyValidators = section.GetSection("StandbyValidators").GetChildren().Select(p => p.Value).ToArray();
            SeedList = section.GetSection("SeedList").GetChildren().Select(p => p.Value).ToArray();
            SystemFee = section.GetSection("SystemFee").GetChildren().ToDictionary(p => (TransactionType)Enum.Parse(typeof(TransactionType), p.Key, true), p => Fixed8.Parse(p.Value));
            SecondsPerBlock = GetValueOrDefault(section.GetSection("SecondsPerBlock"), 15u, p => uint.Parse(p));
            LowPriorityThreshold = GetValueOrDefault(section.GetSection("LowPriorityThreshold"), Fixed8.FromDecimal(0.001m), p => Fixed8.Parse(p));
        }

        internal T GetValueOrDefault<T>(IConfigurationSection section, T defaultValue, Func<string, T> selector)
        {
            if (section.Value == null) return defaultValue;
            return selector(section.Value);
        }
    }
}
