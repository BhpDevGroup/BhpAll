using Bhp.SmartContract;
using Bhp.VM;
using Bhp.Wallets;
using System;
using System.Linq;
using System.Windows.Forms;

namespace Bhp.UI
{
    internal partial class CreateLockAccountDialog : Form
    {
        public CreateLockAccountDialog()
        {
            InitializeComponent();
            comboBox1.Items.AddRange(Program.CurrentWallet.GetAccounts().Where(p => !p.WatchOnly && p.Contract.IsStandardContract()).Select(p => p.GetKey()).ToArray());
        }

        public Contract GetContract()
        {
            uint timestamp = dateTimePicker1.Value.ToTimestamp();
            using (ScriptBuilder sb = new ScriptBuilder())
            {                
                sb.EmitPush(GetKey().PublicKey);
                sb.EmitPush(timestamp);
                // Lock 2.0 in mainnet tx:4e84015258880ced0387f34842b1d96f605b9cc78b308e1f0d876933c2c9134b
                //d3cce84d0800172d09c88ccad61130611bd047a4
                //0x3cb0782aaea35d1f22d5c1e31183932e693b1f26 //bhp
                sb.EmitAppCall(UInt160.Parse("0x3cb0782aaea35d1f22d5c1e31183932e693b1f26"));
                return Contract.Create(new[] { ContractParameterType.Signature }, sb.ToArray());
            }
        }

        public KeyPair GetKey()
        {
            return (KeyPair)comboBox1.SelectedItem;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            button1.Enabled = comboBox1.SelectedIndex >= 0;
        }
    }
}
