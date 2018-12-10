using Bhp.BhpExtensions.Transactions;
using Bhp.BhpExtensions.Wallets;
using Bhp.IO.Json;
using Bhp.Ledger;
using Bhp.Network.P2P.Payloads;
using Bhp.Network.RPC;
using Bhp.Wallets;
using Bhp.Wallets.BRC6;
using Bhp.Wallets.SQLite;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Bhp.BhpExtensions.RPC
{
    /// <summary>
    /// RPC Extension method by BHP
    /// </summary>
    public class RpcExtension
    {
        private Wallet wallet;
        public WalletTimeLock walletTimeLock;
        private bool Unlocking;
        private BhpSystem system;

        public RpcExtension()
        {
            walletTimeLock = new WalletTimeLock(ExtensionSettings.Default.WalletConfig.AutoLock);
            Unlocking = false;
        }

        public RpcExtension(BhpSystem system,Wallet wallet)
        {
            this.system = system;
            this.wallet = wallet;
            walletTimeLock = new WalletTimeLock(ExtensionSettings.Default.WalletConfig.AutoLock);
            Unlocking = false;                        
        }

        public void SetWallet(Wallet wallet)
        {
            this.wallet = wallet;
        }

        public void SetSystem(BhpSystem system)
        {
            this.system = system;
        }

        private Wallet OpenWallet(WalletIndexer indexer, string path, string password)
        {
            if (Path.GetExtension(path) == ".db3")
            {
                return UserWallet.Open(indexer, path, password);
            }
            else
            {
                BRC6Wallet nep6wallet = new BRC6Wallet(indexer, path);
                nep6wallet.Unlock(password);
                return nep6wallet;
            }
        }

        public JObject Process(string method, JArray _params)
        {
            JObject json = new JObject();
             
            switch (method)
            {
                case "unlock":
                    //if (wallet == null) return "wallet is null.";
                    if (ExtensionSettings.Default.WalletConfig.Path.Trim().Length < 1) throw new RpcException(-500, "Wallet file is exists.");
                                        
                    if (_params.Count < 2) throw new RpcException(-501, "parameter is error.");
                    string password = _params[0].AsString();
                    int duration = (int)_params[1].AsNumber();

                    if (Unlocking) { throw new RpcException(-502, "wallet is unlocking...."); }

                    Unlocking = true;
                    try
                    {
                        if (wallet == null)
                        {
                            wallet = OpenWallet(ExtensionSettings.Default.WalletConfig.Indexer, ExtensionSettings.Default.WalletConfig.Path, password);
                            walletTimeLock.SetDuration(wallet == null ? 0 : duration);
                            return $"success";
                        }
                        else
                        {
                            bool ok = walletTimeLock.UnLock(wallet, password, duration);
                            return ok ? "success" : "failure";
                        }
                    }
                    finally
                    {
                        Unlocking = false;
                    }

                case "getutxos":
                    {
                        if (wallet == null || walletTimeLock.IsLocked())
                            throw new RpcException(-400, "Access denied");
                        else
                        {  
                            //address,assetid
                            UInt160 scriptHash = _params[0].AsString().ToScriptHash();
                            IEnumerable<Coin> coins = wallet.FindUnspentCoins();
                            UInt256 assetId;
                            if (_params.Count >= 2)
                            {
                                switch (_params[1].AsString())
                                {
                                    case "bhp":
                                        assetId = Blockchain.GoverningToken.Hash;
                                        break;
                                    case "gas":
                                        assetId = Blockchain.UtilityToken.Hash;
                                        break;
                                    default:
                                        assetId = UInt256.Parse(_params[1].AsString());
                                        break;
                                }
                            }
                            else
                            {
                                assetId = Blockchain.GoverningToken.Hash;
                            }
                            coins = coins.Where(p => p.Output.AssetId.Equals(assetId) && p.Output.ScriptHash.Equals(scriptHash));

                            //json["utxos"] = new JObject();
                            Coin[] coins_array = coins.ToArray();
                            //const int MAX_SHOW = 100;

                            json["utxos"] = new JArray(coins_array.Select(p =>
                            {
                                return p.Reference.ToJson();
                            }));

                            return json;
                        }
                    }

                case "verifytx":
                    { 
                        Transaction tx = Transaction.DeserializeFrom(_params[0].AsString().HexToBytes());
                        string res = VerifyTransaction.Verify(Blockchain.Singleton.GetSnapshot(), new List<Transaction> { tx },tx);

                        json["result"] = res;
                        if ("success".Equals(res))
                        {
                            json["tx"] = tx.ToJson();
                        }
                        return json;
                    }

                case "claimgas":
                    {
                        if (wallet == null || walletTimeLock.IsLocked())
                            throw new RpcException(-400, "Access denied");
                        else
                        { 
                            RpcCoins coins = new RpcCoins(wallet, system);
                            ClaimTransaction[] txs = coins.ClaimAll();
                            if (txs == null)
                            {
                                json["txs"] = new JArray();
                            }
                            else
                            {
                                json["txs"] = new JArray(txs.Select(p =>
                                {
                                    return p.ToJson();
                                }));
                            }
                            return json;
                        }
                    }
                case "showgas":
                    {
                        if (wallet == null || walletTimeLock.IsLocked())
                            throw new RpcException(-400, "Access denied");
                        else
                        {
                            RpcCoins coins = new RpcCoins(wallet, system);
                            json["unavailable"] = coins.UnavailableBonus().ToString();
                            json["available"] = coins.AvailableBonus().ToString();
                            return json;
                        }
                    }
                case "getutxoofaddress":
                case "getaddressutxos":
                    {
                        string from = _params[0].AsString();
                        string jsonRes = RequestRpc("getUtxo",$"address={from}");

                        Newtonsoft.Json.Linq.JArray jsons = (Newtonsoft.Json.Linq.JArray)JsonConvert.DeserializeObject(jsonRes);                        
                        json["utxo"] = new JArray(jsons.Select(p =>
                        {
                            JObject peerJson = new JObject();
                            peerJson["txid"] = p["txid"].ToString();
                            peerJson["n"] = (int)p["n"];
                            peerJson["value"] = (double)p["value"];
                            peerJson["address"] = p["address"].ToString();
                            peerJson["blockHeight"] = (int)p["blockHeight"];
                            return peerJson;
                        }));
                        return json;
                    }

                case "gettransaction":
                    {
                        string from = _params[0].AsString();
                        string position = _params[1].AsString();
                        string offset = _params[2].AsString();
                        string jsonRes = RequestRpc("findTxVout", $"address={from}&position={position}&offset={offset}");
                        
                        Newtonsoft.Json.Linq.JArray jsons = (Newtonsoft.Json.Linq.JArray)JsonConvert.DeserializeObject(jsonRes);
                        json["transaction"] = new JArray(jsons.Select(p =>
                        {
                            JObject peerJson = new JObject();
                            peerJson["txid"] = p["txid"].ToString();
                            peerJson["n"] = (int)p["n"];
                            peerJson["value"] = (double)p["value"];
                            peerJson["address"] = p["address"].ToString();
                            peerJson["asset"] = p["asset"].ToString();
                            return peerJson;
                        }));
                        return json;
                    }

                case "gettxs":
                    {
                        string from = _params[0].AsString();
                        string position = _params[1].AsString();
                        string count = _params[2].AsString();
                        string jsonRes = RequestRpc("gettxs", $"address={from}&position={position}&count={count}");

                        Newtonsoft.Json.Linq.JArray jsons = (Newtonsoft.Json.Linq.JArray)JsonConvert.DeserializeObject(jsonRes);
                        json["tx"] = new JArray(jsons.Select(p =>
                        {
                            JObject peerJson = new JObject();
                            peerJson["txid"] = p["txid"].ToString();
                            peerJson["n"] = (int)p["n"];
                            peerJson["value"] = (double)p["value"];
                            peerJson["address"] = p["address"].ToString();
                            peerJson["blockHeight"] = (int)p["blockHeight"];
                            return peerJson;
                        }));
                        return json;
                    }
                default:
                    throw new RpcException(-32601, "Method not found");
            } 
        }

        private string RequestRpc(string method,string kvs)
        {
            string jsonRes = "";
            using (HttpClient client = new HttpClient())
            {
                string uri = $"{ExtensionSettings.Default.DataRPCServer.Host}/{method}?{kvs}";
                client.BaseAddress = new Uri(uri);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                var response = client.GetAsync(uri).Result;
                Task<Stream> task = response.Content.ReadAsStreamAsync();
                Stream backStream = task.Result;
                StreamReader reader = new StreamReader(backStream);
                jsonRes = reader.ReadToEnd();
                reader.Close();
                backStream.Close();
            }
            return jsonRes;
        }
         
    }
}
