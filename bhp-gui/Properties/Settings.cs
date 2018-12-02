using Microsoft.Extensions.Configuration;
using Bhp.Network.P2P;
using System.Linq;

namespace Bhp.Properties
{
    internal sealed partial class Settings
    {
        public AppConfigs Configs { get; }
        public PathsSettings Paths { get; }
        public P2PSettings P2P { get; }
        public BrowserSettings Urls { get; }
        public ContractSettings Contracts { get; }

        public Settings()
        {
            if (NeedUpgrade)
            {
                Upgrade();
                NeedUpgrade = false;
                Save();
            }
            IConfigurationSection section = new ConfigurationBuilder().AddJsonFile("config.json").Build().GetSection("ApplicationConfiguration");
            this.Configs = new AppConfigs(section.GetSection("AppConfigs"));
            this.Paths = new PathsSettings(section.GetSection("Paths"));
            this.P2P = new P2PSettings(section.GetSection("P2P"));
            this.Urls = new BrowserSettings(section.GetSection("Urls"));
            this.Contracts = new ContractSettings(section.GetSection("Contracts"));
        }
    }

    internal class AppConfigs
    {
        public string Development { get; }
        public AppConfigs(IConfigurationSection section)
        {
            Development = section.GetSection("Development").Value;
        }
    }

    internal class PathsSettings
    {
        public string Chain { get; }
        public string Index { get; }
        public string CertCache { get; }

        public PathsSettings(IConfigurationSection section)
        {
            this.Chain = string.Format(section.GetSection("Chain").Value, Message.Magic.ToString("X8"));
            this.Index = string.Format(section.GetSection("Index").Value, Message.Magic.ToString("X8"));
            this.CertCache = section.GetSection("CertCache").Value;
        }
    }

    internal class P2PSettings
    {
        public ushort Port { get; }
        public ushort WsPort { get; }

        public P2PSettings(IConfigurationSection section)
        {
            this.Port = ushort.Parse(section.GetSection("Port").Value);
            this.WsPort = ushort.Parse(section.GetSection("WsPort").Value);
        }
    }

    internal class BrowserSettings
    {
        public string AddressUrl { get; }
        public string AssetUrl { get; }
        public string TransactionUrl { get; }

        public BrowserSettings(IConfigurationSection section)
        {
            this.AddressUrl = section.GetSection("AddressUrl").Value;
            this.AssetUrl = section.GetSection("AssetUrl").Value;
            this.TransactionUrl = section.GetSection("TransactionUrl").Value;
        }
    }

    internal class ContractSettings
    {
        public UInt160[] BRC20 { get; }

        public ContractSettings(IConfigurationSection section)
        {
            this.BRC20 = section.GetSection("BRC20").GetChildren().Select(p => UInt160.Parse(p.Value)).ToArray();
        }
    }
}
