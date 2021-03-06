﻿namespace Bhp.SmartContract.Framework.Services.Bhp
{
    public class Contract
    {
        public extern byte[] Script
        {
            [Syscall("Bhp.Contract.GetScript")]
            get;
        }
       
        public extern bool IsPayable
        {
            [Syscall("Bhp.Contract.IsPayable")]
            get;
        }

        public extern StorageContext StorageContext
        {
            [Syscall("Bhp.Contract.GetStorageContext")]
            get;
        }

        [Syscall("Bhp.Contract.Create")]
        public static extern Contract Create(byte[] script, byte[] parameter_list, byte return_type, ContractPropertyState contract_property_state, string name, string version, string author, string email, string description);

        [Syscall("Bhp.Contract.Migrate")]
        public static extern Contract Migrate(byte[] script, byte[] parameter_list, byte return_type, ContractPropertyState contract_property_state, string name, string version, string author, string email, string description);

        [Syscall("Bhp.Contract.Destroy")]
        public static extern void Destroy();
    }
}
