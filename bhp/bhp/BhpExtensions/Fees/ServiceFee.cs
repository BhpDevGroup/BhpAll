using Bhp.Ledger;
using Bhp.Network.P2P.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bhp.BhpExtensions.Fees
{
    /// <summary>
    /// ServiceFee of transaction is 0.0001
    /// </summary>
    public class ServiceFee
    {
        public static Fixed8 CalcuServiceFee(List<Transaction> transactions)
        {
            return Fixed8.Zero;
        }

        /*
        public static Fixed8 CalcuServiceFee(List<Transaction> transactions)
        {
            Transaction[] ts = transactions.Where(p => p.Type == TransactionType.ContractTransaction).ToArray();
            Fixed8 inputsum = Fixed8.Zero;
            Fixed8 outputsum = Fixed8.Zero;
            foreach (Transaction tr in ts)
            {
                foreach (CoinReference coin in tr.Inputs)
                {
                    inputsum += Blockchain.Singleton.GetTransaction(coin.PrevHash).Outputs[coin.PrevIndex].Value;
                }
                foreach (TransactionOutput output in tr.Outputs)
                {
                    outputsum += output.Value;
                }
            }
            return inputsum - outputsum;
        }
        */

        public static bool Verify(Transaction tx, TransactionResult[] results_destroy)
        {
            if (results_destroy.Length > 1) return false;
            if (results_destroy.Length == 1 && results_destroy[0].AssetId != Blockchain.UtilityToken.Hash)
                return false;
            return true;
        }

        /*
        public static bool Verify(Transaction tx, TransactionResult[] results_destroy)
        { 
            if (tx.Type == TransactionType.ContractTransaction)
            {
                if (results_destroy.Length > 2) return false;
                if (results_destroy.Length == 1 && results_destroy[0].AssetId != Blockchain.UtilityToken.Hash &&
                    results_destroy[0].AssetId != Blockchain.GoverningToken.Hash)
                    return false;
            }
            else
            {
                if (results_destroy.Length > 1) return false;
                if (results_destroy.Length == 1 && results_destroy[0].AssetId != Blockchain.UtilityToken.Hash)
                    return false;
            }
            return true;
        }
        */

    }
}
