using Bhp.VM;
using System;

namespace Bhp.SmartContract.Framework
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Constructor)]
    public class OpCodeAttribute : Attribute
    {
        public OpCode[] OpCodes { get; }

        public OpCodeAttribute(params OpCode[] opcodes)
        {
            this.OpCodes = opcodes;
        }
    }
}
