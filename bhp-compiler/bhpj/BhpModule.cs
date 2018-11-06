using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Bhp.Compiler
{
    public class BhpModule
    {
        public BhpModule(ILogger logger)
        {
            this.logger = logger;
        }
        ILogger logger;
        //没类型，只有方法
        public SortedDictionary<int, BhpCode> total_Codes = new SortedDictionary<int, BhpCode>();
        public byte[] Build()
        {
            List<byte> bytes = new List<byte>();
            foreach (var c in this.total_Codes.Values)
            {
                bytes.Add((byte)c.code);
                if (c.bytes != null)
                    for (var i = 0; i < c.bytes.Length; i++)
                    {
                        bytes.Add(c.bytes[i]);
                    }
            }
            return bytes.ToArray();
            //将body链接，生成this.code       byte[]
            //并计算 this.codehash            byte[]
        }
        public string mainMethod;
        public ConvOption option;
        public Dictionary<string, BhpMethod> mapMethods = new Dictionary<string, BhpMethod>();
        public Dictionary<string, BhpEvent> mapEvents = new Dictionary<string, BhpEvent>();
        //public Dictionary<string, byte[]> codes = new Dictionary<string, byte[]>();
        //public byte[] GetScript(byte[] script_hash)
        //{
        //    string strhash = "";
        //    foreach (var b in script_hash)
        //    {
        //        strhash += b.ToString("X02");
        //    }
        //    return codes[strhash];
        //}
        public string GenJson()
        {
            MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
            json["__name__"] = new MyJson.JsonNode_ValueString("bhpmodule.");

            //code
            var jsoncode = new MyJson.JsonNode_Array();
            json["code"] = jsoncode;
            foreach (var c in this.total_Codes.Values)
            {
                jsoncode.Add(c.GenJson());
            }
            //code bytes
            var code = this.Build();
            var codestr = "";
            foreach (var c in code)
            {
                codestr += c.ToString("X02");
            }
            json.SetDictValue("codebin", codestr);

            //calls
            MyJson.JsonNode_Object methodinfo = new MyJson.JsonNode_Object();
            json["call"] = methodinfo;
            foreach (var m in this.mapMethods)
            {
                methodinfo[m.Key] = m.Value.GenJson();
            }


            StringBuilder sb = new StringBuilder();
            json.ConvertToStringWithFormat(sb, 4);
            return sb.ToString();
        }
        public void FromJson(string json)
        {

        }
        SHA1 sha1 = SHA1.Create();
        public Dictionary<string, object> staticfields = new Dictionary<string, object>();
       
    }
    public class BhpMethod
    {
        public string _namespace;
        public string name;
        public string displayName;
        public List<BhpParam> paramtypes = new List<BhpParam>();
        public string returntype;
        public bool isPublic = true;
        public bool inSmartContract;
        //临时变量
        public List<BhpParam> body_Variables = new List<BhpParam>();

        //临时记录在此，会合并到一起
        public SortedDictionary<int, BhpCode> body_Codes = new SortedDictionary<int, BhpCode>();
        public int funcaddr;
        public MyJson.JsonNode_Object GenJson()
        {
            MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
            json.SetDictValue("name", this.name);
            var sha1 = SHA1.Create();

            json.SetDictValue("returntype", this.returntype);

            json.SetDictValue("paramcount", this.paramtypes.Count);
            MyJson.JsonNode_Array jsonparams = new MyJson.JsonNode_Array();
            json.SetDictValue("params", jsonparams);
            for (var i = 0; i < this.paramtypes.Count; i++)
            {
                MyJson.JsonNode_Object item = new MyJson.JsonNode_Object();
                item.SetDictValue("name", this.paramtypes[i].name);
                item.SetDictValue("type", this.paramtypes[i].type);
                jsonparams.Add(item);
            }
            return json;
        }
        public void FromJson(MyJson.JsonNode_Object json)
        {

        }
        //public byte[] Build()
        //{
        //    List<byte> bytes = new List<byte>();
        //    foreach (var c in this.body_Codes.Values)
        //    {
        //        bytes.Add((byte)c.code);
        //        if (c.bytes != null)
        //            for (var i = 0; i < c.bytes.Length; i++)
        //            {
        //                bytes.Add(c.bytes[i]);
        //            }
        //    }
        //    return bytes.ToArray();
        //    //将body链接，生成this.code       byte[]
        //    //并计算 this.codehash            byte[]
        //}
        public string lastsfieldname =null;//最后一个加载的静态成员的名字，仅event使用

        public int lastparam = -1;//最后一个加载的参数对应
        public int lastCast = -1;
    }
    public class BhpEvent
    {
        public string _namespace;
        public string name;
        public string displayName;
        public List<BhpParam> paramtypes = new List<BhpParam>();
        public string returntype;
    }

    public class BhpCode
    {
        public VM.OpCode code = VM.OpCode.NOP;
        public int addr;
        public byte[] bytes;
        public string debugcode;
        public int debugline = -1;
        public int debugILAddr = -1;
        public string debugILCode;
        public bool needfix = false;//lateparse tag
        public bool needfixfunc = false;
        public int srcaddr;
        public int[] srcaddrswitch;
        public string srcfunc;
        public override string ToString()
        {
            //string info = "AL_" + addr.ToString("X04") + " " + code.ToString();
            //if (bytes != null)
            //    info += " len=" + bytes.Length;
            //if (debugcode != null && debugline >= 0)
            //{
            //    info += "    " + debugcode + "(" + debugline + ")";
            //}

            string info = "" + addr.ToString("X04") + " " + code.ToString();
            for (var j = 0; j < 16 - code.ToString().Length; j++)
            {
                info += " ";
            }
            info += "[";
            if (bytes != null)
            {
                foreach (var c in bytes)
                {
                    info += c.ToString("X02");
                }
            }
            info += "]";

            if (debugcode != null && debugline >= 0)
            {
                info += "//" + debugcode + "(" + debugline + ")";
            }
            return info;
        }
        public MyJson.JsonNode_ValueString GenJson()
        {
            MyJson.JsonNode_Object json = new MyJson.JsonNode_Object();
            string info = "" + addr.ToString("X04") + " " + code.ToString();
            for (var j = 0; j < 16 - code.ToString().Length; j++)
            {
                info += " ";
            }
            info += "[";
            if (bytes != null)
            {
                foreach (var c in bytes)
                {
                    info += c.ToString("X02");
                }
            }
            info += "]";

            if (debugILCode != null && debugILAddr >= 0)
            {
                info += "<IL_" + debugILAddr.ToString("X04") + " " + debugILCode + ">";
            }
            if (debugcode != null && debugline >= 0)
            {
                info += "//" + debugcode + "(" + debugline + ")";
            }
            return new MyJson.JsonNode_ValueString(info);
        }
        public void FromJson(MyJson.JsonNode_Object json)
        {

        }
    }
    public class BhpParam
    {
        public BhpParam(string name, string type)
        {
            this.name = name;
            this.type = type;
        }
        public string name
        {
            get;
            private set;
        }
        public string type
        {
            get;
            private set;
        }
        public override string ToString()
        {
            return type + " " + name;
        }
    }
}
