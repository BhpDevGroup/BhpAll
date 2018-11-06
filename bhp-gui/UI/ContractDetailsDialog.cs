using Bhp.SmartContract;
using Bhp.Wallets;
using System.Windows.Forms;

namespace Bhp.UI
{
    internal partial class ContractDetailsDialog : Form
    {
        public ContractDetailsDialog(Contract contract)
        {
            InitializeComponent();
            textBox1.Text = contract.ScriptHash.ToAddress();
            textBox2.Text = contract.ScriptHash.ToString();
            textBox3.Text = contract.Script.ToHexString();
        }
    }
}
