﻿using Bhp.Network.P2P.Payloads;
using Bhp.Properties;
using Bhp.SmartContract;
using Bhp.VM;
using Bhp.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Windows.Forms;
using VMArray = Bhp.VM.Types.Array;

namespace Bhp.UI
{
    public partial class TransferDialog : Form
    {
        private string remark = "";

        public Fixed8 Fee => Fixed8.Parse(textBox1.Text);
        public UInt160 ChangeAddress => ((string)comboBox1.SelectedItem).ToScriptHash();

        public TransferDialog()
        {
            InitializeComponent();
            textBox1.Text = "0";
            comboBox1.Items.AddRange(Program.CurrentWallet.GetAccounts().Select(p => p.Address).ToArray());
            comboBox1.SelectedItem = Program.CurrentWallet.GetChangeAddress().ToAddress();
        }

        public Transaction GetTransaction()
        {
            var cOutputs = txOutListBox1.Items.Where(p => p.AssetId is UInt160).GroupBy(p => new
            {
                AssetId = (UInt160)p.AssetId,
                Account = p.ScriptHash
            }, (k, g) => new
            {
                k.AssetId,
                Value = g.Aggregate(BigInteger.Zero, (x, y) => x + y.Value.Value),
                k.Account
            }).ToArray();
            Transaction tx;
            List<TransactionAttribute> attributes = new List<TransactionAttribute>();

            if (LockAttribute != null)//by bhp lock utxo
                attributes.Add(LockAttribute);

            if (cOutputs.Length == 0)
            {
                tx = new ContractTransaction();
            }
            else
            {
                UInt160[] addresses = Program.CurrentWallet.GetAccounts().Select(p => p.ScriptHash).ToArray();
                HashSet<UInt160> sAttributes = new HashSet<UInt160>();
                using (ScriptBuilder sb = new ScriptBuilder())
                {
                    foreach (var output in cOutputs)
                    {
                        byte[] script;
                        using (ScriptBuilder sb2 = new ScriptBuilder())
                        {
                            foreach (UInt160 address in addresses)
                                sb2.EmitAppCall(output.AssetId, "balanceOf", address);
                            sb2.Emit(OpCode.DEPTH, OpCode.PACK);
                            script = sb2.ToArray();
                        }
                        ApplicationEngine engine = ApplicationEngine.Run(script);
                        if (engine.State.HasFlag(VMState.FAULT)) return null;
                        var balances = ((VMArray)engine.ResultStack.Pop()).AsEnumerable().Reverse().Zip(addresses, (i, a) => new
                        {
                            Account = a,
                            Value = i.GetBigInteger()
                        }).ToArray();
                        BigInteger sum = balances.Aggregate(BigInteger.Zero, (x, y) => x + y.Value);
                        if (sum < output.Value) return null;
                        if (sum != output.Value)
                        {
                            balances = balances.OrderByDescending(p => p.Value).ToArray();
                            BigInteger amount = output.Value;
                            int i = 0;
                            while (balances[i].Value <= amount)
                                amount -= balances[i++].Value;
                            if (amount == BigInteger.Zero)
                                balances = balances.Take(i).ToArray();
                            else
                                balances = balances.Take(i).Concat(new[] { balances.Last(p => p.Value >= amount) }).ToArray();
                            sum = balances.Aggregate(BigInteger.Zero, (x, y) => x + y.Value);
                        }
                        sAttributes.UnionWith(balances.Select(p => p.Account));
                        for (int i = 0; i < balances.Length; i++)
                        {
                            BigInteger value = balances[i].Value;
                            if (i == 0)
                            {
                                BigInteger change = sum - output.Value;
                                if (change > 0) value -= change;
                            }
                            sb.EmitAppCall(output.AssetId, "transfer", balances[i].Account, output.Account, value);
                            sb.Emit(OpCode.THROWIFNOT);
                        }
                    }
                    tx = new InvocationTransaction
                    {
                        Version = 1,
                        Script = sb.ToArray()
                    };
                }
                attributes.AddRange(sAttributes.Select(p => new TransactionAttribute
                {
                    Usage = TransactionAttributeUsage.Script,
                    Data = p.ToArray()
                }));
            }
            if (!string.IsNullOrEmpty(remark))
                attributes.Add(new TransactionAttribute
                {
                    Usage = TransactionAttributeUsage.Remark,
                    Data = Encoding.UTF8.GetBytes(remark)
                });
            tx.Attributes = attributes.ToArray();
            tx.Outputs = txOutListBox1.Items.Where(p => p.AssetId is UInt256).Select(p => p.ToTxOutput()).ToArray();
            if (tx is ContractTransaction ctx)
                tx = Program.CurrentWallet.MakeTransaction(ctx, change_address: ChangeAddress, fee: Fee);
            return tx;
        }

        private void txOutListBox1_ItemsChanged(object sender, EventArgs e)
        {
            button3.Enabled = txOutListBox1.ItemCount > 0;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            remark = InputBox.Show(Strings.EnterRemarkMessage, Strings.EnterRemarkTitle, remark);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button2.Visible = false;
            groupBox1.Visible = true;
            this.Height = 510;
        }

        TransactionAttribute LockAttribute = null;
        private void btn_lock_Click(object sender, EventArgs e)
        {
            using (LockUTXODialog dialog = new LockUTXODialog())
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    uint timestamp = dialog.GetUXTOLockTime;
                    using (ScriptBuilder sb = new ScriptBuilder())
                    {
                        sb.EmitPush(timestamp);
                        sb.EmitAppCall(UInt160.Parse("0xc3f09bca040d40714130795121ff7e8477a42690"));// utxo time lock hash
                        LockAttribute = new TransactionAttribute
                        {
                            Usage = TransactionAttributeUsage.SmartContractScript,
                            Data = sb.ToArray()
                        };
                    }
                }
            }
        }
    }
}
