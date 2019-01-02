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
                sb.EmitAppCall(UInt160.Parse("0xe69a2241c0629210c44e37fb03eb786d88a0af21"));// utxo time lock hash
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

            if (tx.Type == TransactionType.ContractTransaction)
            {
                decimal serviceFee = ConstantClass.MinServiceFee;
                int tx_size = tx.Size - tx.Witnesses.Sum(p => p.Size);
                serviceFee = (tx_size / ConstantClass.SizeRadix + (tx_size % ConstantClass.SizeRadix == 0 ? 0 : 1)) * ConstantClass.MinServiceFee; ;
                serviceFee = serviceFee <= ConstantClass.MaxServceFee ? serviceFee : ConstantClass.MaxServceFee;
                TransactionOutput[] tx_changeout = tx.Outputs.Where(p => p.AssetId == Blockchain.GoverningToken.Hash && p.ScriptHash == change_address).OrderByDescending(p => p.Value).ToArray();
                //exist changeaddress
                if (tx_changeout.Count() > 0 && (decimal)tx_changeout[0].Value > serviceFee)
                {
                    tx_changeout[0].Value = Fixed8.FromDecimal((decimal)tx_changeout[0].Value - serviceFee);
                }
                else
                {
                    TransactionOutput[] tx_out = tx.Outputs.Where(p => p.AssetId == Blockchain.GoverningToken.Hash).OrderByDescending(p => p.Value).ToArray();
                    if (tx_out.Count() > 0)
                    {
                        if ((decimal)tx_out[0].Value > serviceFee)
                        {
                            tx_out[0].Value = Fixed8.FromDecimal((decimal)tx_out[0].Value - serviceFee);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
            return tx;
        }
    }
}
