using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using static javaloader.ClassFile.Method;

namespace Bhp.Compiler.JVM
{
    public class JavaModule
    {
        public Dictionary<string, JavaClass> classes = new Dictionary<string, JavaClass>();
        public void LoadClass(string filename, string codepath = null)
        {
            LoadClassByBytes(System.IO.File.ReadAllBytes(filename), codepath);
        }
        public JavaClass LoadClassByBytes(byte[] data, string srccode = null)
        {
            var js = new javaloader.ClassFile(data, 0, data.Length);
            var _class = new JavaClass(this, js, null);
            this.classes[_class.classfile.Name] = _class;
            return _class;
        }
        public void LoadJar(string filename, string codepath = null)
        {
            string f = System.IO.Path.GetFileName(filename);
            //不该基于文件名，而是类的名字
            //if (f == "org.Bhp.smartcontract.framework.jar")
            //{
            //    bskip = true;
            //}
            using (var zipStream = new ZipInputStream(File.OpenRead(filename)))
            {
                ZipEntry ent = null;

                while ((ent = zipStream.GetNextEntry()) != null)
                {
                    var extname = System.IO.Path.GetExtension(ent.Name);
                    if (ent.IsFile && extname == ".class")
                    {
                        byte[] data = null;
                        using (
                        MemoryStream ms = new MemoryStream())
                        {
                            byte[] buf = new byte[2048];
                            int size = buf.Length;
                            while (size > 0)
                            {
                                size = zipStream.Read(buf, 0, buf.Length);
                                if (size > 0)
                                    ms.Write(buf, 0, size);
                            }
                            data = ms.ToArray();
                        }
                        var cc = LoadClassByBytes(data, codepath);
                        var bskip = cc.classfile.Name.IndexOf("org.Bhp.") == 0 || cc.classfile.Name.IndexOf("src.org.Bhp.") == 0;
                        cc.skip = bskip;
                    }

                }
            }



        }



    }
    public class JavaClass
    {
        public Dictionary<string, byte[]> ConstValues = new Dictionary<string, byte[]>();
        public bool IsEnum = false;
        void _InitConsts(Instruction[] Instructions)
        {
            int lastv = -1;
            foreach (var c in Instructions)
            {
                if (c.NormalizedOpCode == javaloader.NormalizedByteCode.__iconst)
                {
                    lastv = c.Arg1;
                }
                else if (c.NormalizedOpCode == javaloader.NormalizedByteCode.__invokespecial)
                {
                    continue;
                }
                else if (c.NormalizedOpCode == javaloader.NormalizedByteCode.__putstatic)
                {
                    var p1 = c.Arg1;
                    if (this.classfile.constantpool[p1] is javaloader.ClassFile.ConstantPoolItemFieldref &&
                       lastv >= 0)
                    {
                        var fref = (javaloader.ClassFile.ConstantPoolItemFieldref)this.classfile.constantpool[p1];
                        this.ConstValues[fref.Name] = new System.Numerics.BigInteger(lastv).ToByteArray();
                    }
                }
                else
                {
                    lastv = -1;
                }
            }
        }
        public JavaModule module;
        public JavaClass(JavaModule module, javaloader.ClassFile classfile, string[] srcfile = null)
        {
            this.module = module;
            this.classfile = classfile;
            if (this.classfile.IsEnum)
            {
                this.IsEnum = true;
                foreach (var m in this.classfile.Methods)
                {
                    if (m.Name == javaloader.StringConstants.CLINIT)
                    {
                        _InitConsts(m.Instructions);
                    }
                }
            }
            this.srcfile = srcfile;
            if (this.srcfile == null)
                this.srcfile = new string[0];
            foreach (var f in this.classfile.Fields)
            {
                this.fields.Add(f.Name, f.Signature);
            }
            bool isKtObj = false;
            if (this.classfile.SourceFileAttribute.Contains(".kt"))
            {
                var sign = "L" + this.classfile.Name + ";";
                foreach (var f in this.classfile.Fields)
                {
                    if (f.Name == "INSTANCE" && f.IsStatic && f.Signature == sign)
                    {
                        isKtObj = true;
                        break;
                    }
                }
            }
            foreach (var m in this.classfile.Methods)
            {

                bool bskip = false;
                if (m.IsStatic == false && isKtObj == false)
                {
                    bskip = true;
                    //静态成员不要，除非是kotlin 的 object 对象，相当于静态

                }

                if (m.Annotations != null)
                {
                    object[] info = m.Annotations[0] as object[];
                    if (info[1] as string == "Lorg/bhp/smartcontract/framework/Appcall;" ||
                        info[1] as string == "Lorg/bhp/smartcontract/framework/Syscall;" ||
                        info[1] as string == "Lorg/bhp/smartcontract/framework/OpCode;" ||
                        info[1] as string == "Lorg/bhp/smartcontract/framework/Nonemit;")
                    {
                        //continue;
                        bskip = true;
                    }
                    //if(m.Annotations[0])
                }
                if (m.Name == "<init>")
                    bskip = true;
                var nm = new JavaMethod(this, m);
                nm.skip = bskip;
                //if (bskip == false && methods.ContainsKey(m.Name))
                //{
                //    throw new Exception("already have a func named:" + classfile.Name + "." + m.Name);
                //}
                this.methods[m.Name + m.Signature] = nm;
            }
            this.superClass = this.classfile.SuperClass;
        }
        public bool skip = false;
        public string[] srcfile;
        public string superClass;
        public javaloader.ClassFile classfile;
        public Dictionary<string, string> fields = new Dictionary<string, string>();
        public Dictionary<string, JavaMethod> methods = new Dictionary<string, JavaMethod>();

    }
    public class JavaMethod
    {
        public bool skip = false;
        public JavaClass DeclaringType;
        public javaloader.ClassFile.Method method;
        public string returnType;
        public List<string> paramTypes = new List<string>();
        public Dictionary<int, OpCode> body_Codes = new Dictionary<int, OpCode>();
        public List<BhpParam> body_Variables = new List<BhpParam>();

        public int MaxVariableIndex = 0;
        //public int addLocal_VariablesCount = 0;
        //不做表转换了，直接按最大索引给
        public Dictionary<int, int> argTable;// new List<int>();//index->arg index
                                             //public Dictionary<int, int> localTable;//index->localIndex;

        public JavaMethod(JavaClass type, javaloader.ClassFile.Method method)
        {
            this.DeclaringType = type;
            this.method = method;
            //method.LocalVariableTableAttribute
            this.argTable = new Dictionary<int, int>();
            //this.localTable = new Dictionary<int, int>();
            if (method.ArgMap != null)
                for (var i = 0; i < method.ArgMap.Length; i++)
                {
                    var ind = method.ArgMap[i];
                    if (ind >= 0)
                        this.argTable[ind] = i;
                }
            scanTypes(method.Signature, out this.returnType, this.paramTypes);
            Dictionary<int, string> local = new Dictionary<int, string>();

            if (this.method.LocalVariableTableAttribute != null)
                foreach (var lv in this.method.LocalVariableTableAttribute)
                {
                    var ind = lv.index;
                    if (this.argTable.ContainsValue(ind) == false)
                    {

                        var desc = lv.name + ";" + lv.descriptor;
                        if (local.ContainsKey(ind))
                        {
                            local[ind] = local[ind] + "||" + desc;
                        }
                        else
                        {
                            local[ind] = desc;
                        }
                    }
                    this.MaxVariableIndex = Math.Max(ind + 1, this.MaxVariableIndex);
                }
            //for (var i = 0; i < local.Count; i++)
            //{
            //    this.localTable[local.Keys.ToArray()[i]] = i;
            //}

            {
                this.body_Variables = new List<BhpParam>();

                //var addLocal_VariablesCount = this.method.MaxLocals - this.paramTypes.Count;
                //if (addLocal_VariablesCount < local.Count)
                //{
                //    throw new Exception("not impossible.");
                //}
                //for (var i = 0; i < addLocal_VariablesCount; i++)
                //{
                //    this.body_Variables.Add(new Param("_noname", ""));
                //}

                for (var i = 0; i < MaxVariableIndex; i++)
                {
                    this.body_Variables.Add(new BhpParam("_noname", ""));
                }
                foreach (var lv in local)
                {
                    this.body_Variables[lv.Key - this.paramTypes.Count] = new BhpParam("local", lv.Value);
                }
            }
            if (this.method.Instructions != null)
                for (var i = 0; i < this.method.Instructions.Length; i++)
                {
                    Instruction code = this.method.Instructions[i];
                    var opcode = new OpCode();

                    opcode.InitToken(this, code);
                    this.body_Codes[code.PC] = opcode;
                }
            // this.method.LocalVariableTableAttribute

        }
        static string getTypeString(string sign, ref int i)
        {
            if (sign[i] == '[') //for array
            {
                i++;
                return "[" + getTypeString(sign, ref i);
            }
            else if (sign[i] == 'V')
            {
                return "void";
            }
            else if (sign[i] == 'I') //a int
            {
                return "int";
            }
            else if (sign[i] == 'J') //a long
            {
                return "long";
            }
            else if (sign[i] == 'B')
            {
                return "byte";
            }
            else if (sign[i] == 'S')
            {
                return "short";
            }
            else if (sign[i] == 'Z')
            {
                return "boolean";
            }
            else if (sign[i] == 'F')
            {
                return "float";
            }
            else if (sign[i] == 'D')
            {
                return "double";
            }
            else if (sign[i] == 'C')
            {
                return "char";
            }
            else if (sign[i] == 'L')//a long string
            {
                var i2 = sign.IndexOf(';', i);

                var type = sign.Substring(i + 1, i2 - i - 1);

                i = i2;
                return type;
            }
            else
            {
                throw new Exception("not parsed sign.");
            }
        }
        public static void scanTypes(string sign, out string returnType, List<string> paramTypes)
        {
            returnType = "";
            bool forreturn = false;
            for (var i = 0; i < sign.Length; i++)
            {

                if (sign[i] == '(') //beginparam
                {
                    continue;
                }
                else if (sign[i] == ')')//endparam
                {
                    forreturn = true;
                    continue;
                }
                else
                {
                    string type = getTypeString(sign, ref i);
                    if (forreturn)
                    {
                        returnType = type;
                        return;
                    }
                    else
                    {
                        paramTypes.Add(type);
                        continue;
                    }

                }
            }
        }
        //void scanTypes(string sign)
        //{
        //    bool forreturn = false;
        //    for (var i = 0; i < sign.Length; i++)
        //    {

        //        if (sign[i] == '(') //beginparam
        //        {
        //            continue;
        //        }
        //        else if (sign[i] == ')')//endparam
        //        {
        //            forreturn = true;
        //            continue;
        //        }
        //        else
        //        {
        //            string type = getTypeString(sign, ref i);
        //            if (forreturn)
        //            {
        //                returnType = type;
        //                return;
        //            }
        //            else
        //            {
        //                paramTypes.Add(type);
        //                continue;
        //            }

        //        }
        //    }
        //}
        public int GetLastCodeAddr(int srcaddr)
        {
            int last = -1;
            foreach (var key in this.body_Codes.Keys)
            {
                if (key == srcaddr)
                {

                    return last;
                }
                last = key;
            }
            return last;
        }
        public int GetNextCodeAddr(int srcaddr)
        {
            bool bskip = false;
            foreach (var key in this.body_Codes.Keys)
            {
                if (key == srcaddr)
                {
                    bskip = true;
                    continue;
                }
                if (bskip)
                {
                    return key;
                }

            }
            return -1;
        }
    }

    public class OpCode
    {
        public javaloader.NormalizedByteCode code;
        public override string ToString()
        {
            var info = "IL_" + addr.ToString("X04") + " " + code + " ";
            if (this.tokenValueType == TokenValueType.Method)
                info += tokenMethod;
            if (this.tokenValueType == TokenValueType.String)
                info += tokenStr;

            if (debugline >= 0)
            {
                info += "(" + debugline + ")";
            }
            return info;
        }
        public enum TokenValueType
        {
            Nothing,
            Addr,//地址
            AddrArray,
            String,
            Type,
            Field,
            Method,
            I32,
            I64,
            OTher,
        }
        public TokenValueType tokenValueType = TokenValueType.Nothing;
        public int addr;
        public int debugline = -1;
        public string debugcode;
        public int arg1;
        public int arg2;

        public object tokenUnknown;
        public int tokenAddr_Index;
        //public int tokenAddr;
        public int[] tokenAddr_Switch;
        public string tokenType;
        public string tokenField;
        public string tokenMethod;
        public int tokenI32;
        public Int64 tokenI64;
        public float tokenR32;
        public double tokenR64;
        public string tokenStr;
        public void InitToken(JavaMethod method, Instruction ins)
        {
            this.code = ins.NormalizedOpCode;
            this.arg1 = ins.Arg1;
            this.arg2 = ins.Arg2;
            this.addr = ins.PC;
            if (method.method.LineNumberTableAttribute == null || method.method.LineNumberTableAttribute.TryGetValue(this.addr, out this.debugline) == false)
            {
                this.debugline = -1;
            }
            if (this.debugline >= 0)
            {
                if (this.debugline - 1 < method.DeclaringType.srcfile.Length)
                    this.debugcode = method.DeclaringType.srcfile[this.debugline - 1];
            }
            switch (code)
            {
                case javaloader.NormalizedByteCode.__iconst:
                    this.tokenI32 = this.arg1;
                    break;
                //case javaloader.NormalizedByteCode.__newarray:
                //    var c = method.DeclaringType.classfile.constantpool[this.arg1];
                //    break;
                case javaloader.NormalizedByteCode.__astore:
                    break;
                default:
                    this.tokenUnknown = ins;
                    this.tokenValueType = TokenValueType.Nothing;
                    break;
            }
        }

    }
}
