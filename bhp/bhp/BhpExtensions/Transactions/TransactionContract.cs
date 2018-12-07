using Bhp.Ledger;
using Bhp.Network.P2P.Payloads;
using Bhp.VM;
using Bhp.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bhp.BhpExtensions.Transactions
{
    public class TransactionContract
    {
        public TransactionAttribute MakeLockTransactionScript(uint timestamp)
        { 
            using (ScriptBuilder sb = new ScriptBuilder())
            {
                sb.EmitPush(timestamp);
                sb.EmitAppCall(UInt160.Parse("0xc3f09bca040d40714130795121ff7e8477a42690"));// utxo time lock hash
                return new TransactionAttribute
                {
                    Usage = TransactionAttributeUsage.SmartContractScript,
                    Data = sb.ToArray()
                };
            }
        }

        public T MakeTransaction<T>(Wallet wallet,T tx, UInt160 from = null, UInt160 change_address = null, Fixed8 fee = default(Fixed8), Fixed8 transaction_fee = default(Fixed8)) where T : Transaction
        {
            if (tx.Outputs == null) tx.Outputs = new TransactionOutput[0];
            if (tx.Attributes == null) tx.Attributes = new TransactionAttribute[0];
            fee += tx.SystemFee;
            var pay_total = (typeof(T) == typeof(IssueTransaction) ? new TransactionOutput[0] : tx.Outputs).GroupBy(p => p.AssetId, (k, g) => new
            {
                AssetId = k,
                Value = g.Sum(p => p.Value)
            }).ToDictionary(p => p.AssetId);
            if (fee > Fixed8.Zero)
            {
                if (pay_total.ContainsKey(Blockchain.UtilityToken.Hash))
                {
                    pay_total[Blockchain.UtilityToken.Hash] = new
                    {
                        AssetId = Blockchain.UtilityToken.Hash,
                        Value = pay_total[Blockchain.UtilityToken.Hash].Value + fee
                    };
                }
                else
                {
                    pay_total.Add(Blockchain.UtilityToken.Hash, new
                    {
                        AssetId = Blockchain.UtilityToken.Hash,
                        Value = fee
                    });
                }
            }
            var pay_coins = pay_total.Select(p => new
            {
                AssetId = p.Key,
                Unspents = from == null ? wallet.FindUnspentCoins(p.Key, p.Value.Value) : wallet.FindUnspentCoins(p.Key, p.Value.Value, from)
            }).ToDictionary(p => p.AssetId);
            if (pay_coins.Any(p => p.Value.Unspents == null)) return null;
            var input_sum = pay_coins.Values.ToDictionary(p => p.AssetId, p => new
            {
                p.AssetId,
                Value = p.Unspents.Sum(q => q.Output.Value)
            });
            if (change_address == null) change_address = wallet.GetChangeAddress();
            List<TransactionOutput> outputs_new = new List<TransactionOutput>(tx.Outputs);
            foreach (UInt256 asset_id in input_sum.Keys)
            {
                bool isChange = false;
                if (input_sum[asset_id].Value > pay_total[asset_id].Value)
                {
                    isChange = true;
                    outputs_new.Add(new TransactionOutput
                    {
                        AssetId = asset_id,
                        Value = input_sum[asset_id].Value - pay_total[asset_id].Value,
                        ScriptHash = change_address
                    });
                }

                //By BHP
                if (tx.Attributes.Length > 0)
                {
                    for (int i = 0; i < tx.Attributes.Length; i++)
                    {
                        if (tx.Attributes[i].Usage == TransactionAttributeUsage.SmartContractScript)
                        {
                            int n = -1;
                            if (isChange)
                            {
                                n = outputs_new.Count - 1;
                            }
                            using (ScriptBuilder sb = new ScriptBuilder())
                            {
                                sb.EmitPush(n);
                                sb.EmitPush(tx.Attributes[i].Data);
                                tx.Attributes[i].Data = sb.ToArray();
                            }
                        }
                    }
                }
            }
            tx.Inputs = pay_coins.Values.SelectMany(p => p.Unspents).Select(p => p.Reference).ToArray();
            tx.Outputs = outputs_new.ToArray();

            decimal Rate = 0.0001m;
            if (tx.Type == TransactionType.ContractTransaction)
            {
                foreach (TransactionOutput txo in tx.Outputs)
                {
                    if ((decimal)txo.Value < 0.0001m)
                    {
                        throw new Exception("-500, TransactionFee not Enough");
                    }
                    else
                    {
                        txo.Value = Fixed8.FromDecimal((decimal)txo.Value - ((decimal)txo.Value * Rate));
                    }
                }
            }
            return tx;
        }
    }
}
