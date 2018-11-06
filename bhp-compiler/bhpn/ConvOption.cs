using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bhp.Compiler
{
    public class ConvOption
    {
        public bool useNep8 = false;//call to callI

        public bool useSysCallInteropHash = false;
        public static ConvOption Default
        {
            get
            {
                return new ConvOption();
            }
        }
    }
}
