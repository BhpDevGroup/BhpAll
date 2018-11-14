using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Bhp;
using Bhp.Cryptography;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        { 
            string smessage = "80000001fded20e42b75a1263268098aa0e1dfd0baad2fb3bf2edaa3480ba7aee2440edd00000254a80a4c72f6157a7af0a753fc4ac4af6b159a17634dd57fecf319feab6ff7130088526a740000008d8dfca8e949d5548e3193b88b59d682e3426b1c54a80a4c72f6157a7af0a753fc4ac4af6b159a17634dd57fecf319feab6ff7130088526a7400000015a40075f96efb432420b1d599dc5b3a52dca5e0";
            string spublickey = "03d1998c163fc5137bb2ccc8062968db728081857ecf9d5c489c93935ab1aeece9";
            string sprivatekey = "68a1639fa801be966c6da92570f7a24caa26618061b4a3248b03247d7fef418a";
            byte[] message = smessage.HexToBytes();
            byte[] publickey = spublickey.HexToBytes();
            byte[] privatekey = sprivatekey.HexToBytes();

           
            byte[] signature = Crypto.Default.Sign(message, privatekey, publickey);
            string str = signature.ToHexString();

            Console.WriteLine(str);
            Console.ReadLine();
        }
    }
}
