using System;
using System.Numerics;

namespace Bhp.Compiler.JVM
{
    public partial class ModuleConverter
    {
        private BhpCode _Insert1(VM.OpCode code, string comment, BhpMethod to, byte[] data = null)
        {
            BhpCode _code = new BhpCode();
            int startaddr = addr;
            _code.addr = addr;
            {
                _code.debugcode = comment;
                _code.debugline = 0;
            }

            addr++;

            _code.code = code;

            if (data != null)
            {
                _code.bytes = data;
                addr += _code.bytes.Length;
            }
            to.body_Codes[startaddr] = _code;
            return _code;
        }

        private BhpCode _InsertPush(byte[] data, string comment, BhpMethod to)
        {
            if (data.Length == 0) return _Insert1(VM.OpCode.PUSH0, comment, to);
            if (data.Length <= 75) return _Insert1((VM.OpCode)data.Length, comment, to, data);
            byte prefixLen;
            VM.OpCode code;
            if (data.Length <= byte.MaxValue)
            {
                prefixLen = sizeof(byte);
                code = VM.OpCode.PUSHDATA1;
            }
            else if (data.Length <= ushort.MaxValue)
            {
                prefixLen = sizeof(ushort);
                code = VM.OpCode.PUSHDATA2;
            }
            else
            {
                prefixLen = sizeof(uint);
                code = VM.OpCode.PUSHDATA4;
            }
            byte[] bytes = new byte[data.Length + prefixLen];
            Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, bytes, 0, prefixLen);
            Buffer.BlockCopy(data, 0, bytes, prefixLen, data.Length);
            return _Insert1(code, comment, to, bytes);
        }

        private BhpCode _InsertPush(int i, string comment, BhpMethod to)
        {
            if (i == 0) return _Insert1(VM.OpCode.PUSH0, comment, to);
            if (i == -1) return _Insert1(VM.OpCode.PUSHM1, comment, to);
            if (i > 0 && i <= 16) return _Insert1((VM.OpCode)(byte)i + 0x50, comment, to);
            return _InsertPush(((BigInteger)i).ToByteArray(), comment, to);
        }

        private BhpCode _Convert1by1(VM.OpCode code, OpCode src, BhpMethod to, byte[] data = null)
        {
            BhpCode _code = new BhpCode();
            int startaddr = addr;
            _code.addr = addr;
            if (src != null)
            {
                addrconv[src.addr] = addr;
                _code.debugcode = src.debugcode;
                _code.debugline = src.debugline;
                _code.debugILAddr = src.addr;
                _code.debugILCode = src.code.ToString();
            }


            addr++;

            _code.code = code;

            if (data != null)
            {
                _code.bytes = data;
                addr += _code.bytes.Length;
            }
            to.body_Codes[startaddr] = _code;
            return _code;
        }

        private BhpCode _ConvertPush(byte[] data, OpCode src, BhpMethod to)
        {
            if (data.Length == 0) return _Convert1by1(VM.OpCode.PUSH0, src, to);
            //if (data.Length <= 75) return _Convert1by1((VM.OpCode)data.Length, src, to, data);
            byte prefixLen;
            VM.OpCode code;
            if (data.Length <= byte.MaxValue)
            {
                prefixLen = sizeof(byte);
                code = VM.OpCode.PUSHDATA1;
            }
            else if (data.Length <= ushort.MaxValue)
            {
                prefixLen = sizeof(ushort);
                code = VM.OpCode.PUSHDATA2;
            }
            else
            {
                prefixLen = sizeof(uint);
                code = VM.OpCode.PUSHDATA4;
            }
            byte[] bytes = new byte[data.Length + prefixLen];
            Buffer.BlockCopy(BitConverter.GetBytes(data.Length), 0, bytes, 0, prefixLen);
            Buffer.BlockCopy(data, 0, bytes, prefixLen, data.Length);
            return _Convert1by1(code, src, to, bytes);
        }

        private BhpCode _ConvertPush(long i, OpCode src, BhpMethod to)
        {
            if (i == 0) return _Convert1by1(VM.OpCode.PUSH0, src, to);
            if (i == -1) return _Convert1by1(VM.OpCode.PUSHM1, src, to);
            if (i > 0 && i <= 16) return _Convert1by1((VM.OpCode)(byte)i + 0x50, src, to);
            return _ConvertPush(((BigInteger)i).ToByteArray(), src, to);
        }

        private void _insertBeginCode(JavaMethod from, BhpMethod to)
        {
            //压入槽位栈
            _InsertPush(from.MaxVariableIndex + 1, "begincode", to);
            _Insert1(VM.OpCode.NEWARRAY, "", to);
            _Insert1(VM.OpCode.TOALTSTACK, "", to);

            for (var i = 0; i < from.paramTypes.Count; i++)
            {
                int pos = 0;
                if (from.method.IsStatic)
                {
                    pos = from.argTable[i];
                }
                else
                {//非静态0号是this
                    pos = from.argTable[i + 1];
                }
                _Insert1(VM.OpCode.DUPFROMALTSTACK, "init param:" + i, to);
                _InsertPush(pos, "", to);
                _InsertPush(2, "", to);
                _Insert1(VM.OpCode.ROLL, "", to);
                _Insert1(VM.OpCode.SETITEM, "", to);
            }
            ////初始化临时槽位位置
            //to.addVariablesCount = from.addLocal_VariablesCount;
            //for (var i = 0; i < from.addLocal_VariablesCount; i++)
            //{
            //    //to.body_Variables.Add(new JavaParam(src.name, src.type));
            //    _InsertPush(0, "body_Variables init", to);
            //}
        }

        private void _insertEndCode(JavaMethod from, BhpMethod to, OpCode src)
        {
            //占位不谢
            //_Convert1by1(VM.OpCode.NOP, src, to);

            ////移除临时槽位
            ////drop body_Variables
            //for (var i = 0; i < from.addLocal_VariablesCount; i++)
            //{
            //    _Insert1(VM.OpCode.DEPTH, "body_Variables drop", to, null);
            //    _Insert1(VM.OpCode.DEC, null, to, null);

            //    //push olddepth
            //    _Insert1(VM.OpCode.FROMALTSTACK, null, to);
            //    _Insert1(VM.OpCode.DUP, null, to);
            //    _Insert1(VM.OpCode.TOALTSTACK, null, to);
            //    //(d-1)-olddepth
            //    _Insert1(VM.OpCode.SUB, null, to);

            //    _Insert1(VM.OpCode.XDROP, null, to, null);
            //}
            ////移除参数槽位
            //for (var i = 0; i < from.paramTypes.Count; i++)
            //{
            //    //d
            //    _Insert1(VM.OpCode.DEPTH, "param drop", to, null);

            //    //push olddepth
            //    _Insert1(VM.OpCode.FROMALTSTACK, null, to);
            //    _Insert1(VM.OpCode.DUP, null, to);
            //    _Insert1(VM.OpCode.DEC, null, to);//深度-1
            //    _Insert1(VM.OpCode.TOALTSTACK, null, to);

            //    //(d)-olddepth
            //    _Insert1(VM.OpCode.SUB, null, to);

            //    _Insert1(VM.OpCode.XDROP, null, to, null);

            //}

            //移除深度临时栈
            _Insert1(VM.OpCode.FROMALTSTACK, "", to);
            _Insert1(VM.OpCode.DROP, "", to);
        }
    }
}
