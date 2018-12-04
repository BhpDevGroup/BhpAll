using Akka.Actor;
using Bhp;
using Bhp.Ledger;
using Bhp.Mining;
using Bhp.Network.P2P;
using Bhp.Network.P2P.Payloads;
using Bhp.Persistence.LevelDB;
using Bhp.SmartContract;
using Bhp.Wallets.BRC6;
using System;
 

namespace TestTransaction
{
    class Program
    {
        static void Mining(BhpSystem system)
        {
            Fixed8 amount_netfee = Fixed8.Zero;
            Fixed8 transaction_fee = Fixed8.Zero;
            MiningTransaction miningTransaction = new MiningTransaction(amount_netfee);

            ulong nonce = 100156895;

            BRC6Wallet wallet = new BRC6Wallet(new Bhp.Wallets.WalletIndexer(@"walletindex"), @"D:\BHP\Test\t1.json");
            wallet.Unlock("1");
            wallet.WalletTransaction += Wallet_WalletTransaction;
            MinerTransaction tx = miningTransaction.GetMinerTransaction(nonce, 100, wallet);
            Console.WriteLine(tx.ToJson());
            
            Console.WriteLine("\n Staring Sign......");
            ContractParametersContext context = new ContractParametersContext(tx);
            wallet.Sign(context);
            if (context.Completed)
            {
                Console.WriteLine("\n Sign successfully.");
                context.Verifiable.Witnesses = context.GetWitnesses();
                string hexString = context.Verifiable.GetHashData().ToHexString();
                Console.WriteLine($"\n {hexString}");
                system.LocalNode.Tell(new LocalNode.Relay { Inventory = tx });

                //RelayResultReason reason = system.Blockchain.Ask<RelayResultReason>(tx).Result;
                //Console.WriteLine("\n relay tx: " + reason);
            } 
        }

        private static void Wallet_WalletTransaction(object sender, Bhp.Wallets.WalletTransactionEventArgs e)
        {
            Console.WriteLine($"  Block Height: {Blockchain.Singleton.Height}, Wallet Height: {e.Height}");
        }



        static void Main(string[] args)
        {
            BhpSystem system = new BhpSystem(new LevelDBStore(@"db"));            
            system.StartNode(10555,10556);
            
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 5000;
            timer.Elapsed += Timer_Elapsed;
            timer.Start();

            Console.WriteLine(" Please press any key starting...");
            Console.ReadLine();
            Console.WriteLine(" Start Mining...\n");

             

            Mining(system); 
            Console.WriteLine("\n The end of Mining.");
            string line = Console.ReadLine();
            Console.WriteLine(line);
        }

        private static void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Console.WriteLine(" block height : " + Blockchain.Singleton.Height);
           
        }
    }
}
