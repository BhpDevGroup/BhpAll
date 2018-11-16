using Bhp.SmartContract.Framework;
using Bhp.SmartContract.Framework.Services.Bhp;
 

namespace BhpContract1
{
    public class Contract1 : SmartContract
    {
        public static bool Main(string operation, object[] args)
        {
            byte[] key = "BhpLock".AsByteArray();
            byte[] value = "Hello Bhp".AsByteArray();
            Storage.Put(Storage.CurrentContext, key, value);
            return true;
        }
    }
}
