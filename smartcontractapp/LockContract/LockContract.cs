using Bhp.SmartContract.Framework;
using Bhp.SmartContract.Framework.Services.Bhp;

namespace LockContract
{
    public class LockContract : SmartContract
    {
        public static bool Main(uint timestamp, byte[] pubkey, byte[] signature)
        {
            Header header = Blockchain.GetHeader(Blockchain.GetHeight());
            if (header.Timestamp < timestamp)
                return false;
            return VerifySignature(signature, pubkey);
        }
    }
}
