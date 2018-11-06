using Bhp.Network.P2P.Payloads;
using System.ComponentModel;

namespace Bhp.UI.Wrappers
{
    internal class TransactionAttributeWrapper
    {
        public TransactionAttributeUsage Usage { get; set; }
        [TypeConverter(typeof(HexConverter))]
        public byte[] Data { get; set; }

        public TransactionAttribute Unwrap()
        {
            return new TransactionAttribute
            {
                Usage = Usage,
                Data = Data
            };
        }
    }
}
