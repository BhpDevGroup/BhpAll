using Bhp.SmartContract.Framework;
using Bhp.SmartContract.Framework.Services.Bhp;  
 

namespace BhpContract2
{
    public class Contract1 : SmartContract
    {
        public static bool Main(string operation, object[] args)
        {
            //Storage.Put(Storage.CurrentContext, "Hello", "World");             
            byte[] value = Storage.Get(Storage.CurrentContext, "BhpLock".AsByteArray());
            string str = value.AsString(); 
            return value != null;
        }
    }
}
