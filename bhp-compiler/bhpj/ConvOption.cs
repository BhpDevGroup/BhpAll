
namespace Bhp.Compiler
{
    public class ConvOption
    {
        public bool useNep8 = false;//call to callI
        public static ConvOption Default
        {
            get
            {
                return new ConvOption();
            }
        }
    }
}
