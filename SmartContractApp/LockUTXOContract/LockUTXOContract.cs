using Bhp.SmartContract.Framework;
using Bhp.SmartContract.Framework.Services.Bhp;
using System;
using System.Numerics;

namespace LockUTXOContract
{
    public class LockUTXOContract : SmartContract
    {
        public static bool Main(uint timestamp)
        {
            Header header = Blockchain.GetHeader(Blockchain.GetHeight());
            if (header.Timestamp < timestamp)
                return false;
            return true; 
        }
    }
}
