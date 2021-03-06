﻿using Bhp.Ledger;
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
            //return Fixed8.Zero;
            Transaction[] ts = transactions.Where(p => p.Type == TransactionType.ContractTransaction).ToArray();
            Fixed8 inputsum = Fixed8.Zero;
            Fixed8 outputsum = Fixed8.Zero;
            foreach (Transaction tr in ts)
            {
                foreach (CoinReference coin in tr.Inputs)
                {
                    if (Blockchain.Singleton.GetTransaction(coin.PrevHash).Outputs[coin.PrevIndex].AssetId == Blockchain.GoverningToken.Hash)
                    {
                       inputsum += Blockchain.Singleton.GetTransaction(coin.PrevHash).Outputs[coin.PrevIndex].Value;
                    }
                }
                foreach (TransactionOutput output in tr.Outputs.Where(p => p.AssetId == Blockchain.GoverningToken.Hash))
                {
                    outputsum += output.Value;
                }
            }
            return inputsum - outputsum;

        }

        //public static bool Verify(Transaction tx, TransactionResult[] results_destroy,Fixed8 systemFee)
        //{
        //    if (results_destroy.Length > 1) return false;
        //    if (results_destroy.Length == 1 && results_destroy[0].AssetId != Blockchain.UtilityToken.Hash)
        //        return false;
        //    return true;
        //}
        
        public static bool Verify(Transaction tx, TransactionResult[] results_destroy, Fixed8 SystemFee)
        {
            if (tx.Type == TransactionType.ContractTransaction)
            {
                Fixed8 otherInput = Fixed8.Zero;
                otherInput = tx.References.Values.Where(p => p.AssetId != Blockchain.UtilityToken.Hash).Sum(p => p.Value);
                //输入存在不是gas的资产
                if (otherInput == Fixed8.Zero)
                {
                    if (results_destroy.Length == 0) return true;
                    if (results_destroy.Length == 1 && results_destroy[0].AssetId != Blockchain.UtilityToken.Hash) return false;
                    if (results_destroy.Length > 1) return false;

                    Fixed8 amount = results_destroy.Where(p => p.AssetId == Blockchain.UtilityToken.Hash).Sum(p => p.Amount);
                    if (SystemFee > Fixed8.Zero && amount < SystemFee) return false;
                    
                    return true;
                }
                else
                {
                    if (results_destroy.Length == 0 || results_destroy.Length > 2) return false;
                    if (results_destroy.Length == 1 && results_destroy[0].AssetId != Blockchain.GoverningToken.Hash) return false;
                    if (results_destroy.Any(p => p.AssetId != Blockchain.GoverningToken.Hash && p.AssetId != Blockchain.UtilityToken.Hash)) return false;

                    //verify gas
                    Fixed8 amount = results_destroy.Where(p => p.AssetId == Blockchain.UtilityToken.Hash).Sum(p => p.Amount);
                    if (SystemFee > Fixed8.Zero && amount < SystemFee) return false;
                    return CheckServiceFee(tx);
                }
            }
            else
            {
                if (results_destroy.Length > 1) return false;
                if (results_destroy.Length == 1 && results_destroy[0].AssetId != Blockchain.UtilityToken.Hash)
                    return false;
                if (SystemFee > Fixed8.Zero && (results_destroy.Length == 0 || results_destroy[0].Amount < SystemFee))
                    return false;
                return true;
            }
        }

        //By BHP
        public static bool CheckServiceFee(Transaction tx)
        {
            /* 万分之一手续费验证
            if (tx.References == null) return false;
            Fixed8 inputSum = tx.References.Values.Where(p => p.AssetId == Blockchain.GoverningToken.Hash).Sum(p => p.Value);
            Fixed8 outputSum = tx.Outputs.Where(p => p.AssetId == Blockchain.GoverningToken.Hash).Sum(p => p.Value);
            decimal serviceFee = (decimal)inputSum * 0.0001m;
            decimal payFee = (decimal)inputSum - (decimal)outputSum;
            return payFee >= serviceFee;*/

            if (tx.References == null) return false;
            Fixed8 inputSum = tx.References.Values.Where(p => p.AssetId == Blockchain.GoverningToken.Hash).Sum(p => p.Value);
            Fixed8 outputSum = tx.Outputs.Where(p => p.AssetId == Blockchain.GoverningToken.Hash).Sum(p => p.Value);
            if (inputSum != Fixed8.Zero)
            {
                decimal serviceFee = ConstantClass.MinServiceFee;
                int tx_size = tx.Size - tx.Witnesses.Sum(p => p.Size);
                serviceFee = (tx_size / ConstantClass.SizeRadix + (tx_size % ConstantClass.SizeRadix == 0 ? 0:1)) * ConstantClass.MinServiceFee; ;
                serviceFee = serviceFee <= ConstantClass.MaxServceFee ? serviceFee : ConstantClass.MaxServceFee ;
                decimal payFee = (decimal)inputSum - (decimal)outputSum;
                return payFee >= serviceFee;
            }
            return true;
        }
    }
}
