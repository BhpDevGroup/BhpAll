using System;
using System.Collections.Generic;
using System.Text;

namespace Bhp
{
    internal class ConstantClass
    {
        /// <summary>
        /// 手续费收取字节基数
        /// </summary>
        public const int SizeRadix = 512;

        /// <summary>
        /// 最小手续费
        /// </summary>
        public const decimal MinServiceFee = 0.0001m;
        
        /// <summary>
        /// 最大手续费
        /// </summary>
        public const decimal MaxServceFee = 0.0005m;
        
    }
}
