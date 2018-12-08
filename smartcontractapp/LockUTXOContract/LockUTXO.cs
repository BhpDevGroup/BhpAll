using Bhp.SmartContract.Framework;
using Bhp.SmartContract.Framework.Services.Bhp; 

namespace LockUTXOContract
{
    public class LockUTXO : SmartContract
    {
        public static bool Main(uint timestamp)
        {
            Header header = Blockchain.GetHeader(Blockchain.GetHeight());
            if (header.Timestamp < timestamp)
                return false;
            return true;
        }
    }//end of class
}
