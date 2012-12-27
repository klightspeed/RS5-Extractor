using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RS5_Extractor
{
    public class RS5Environment
    {
        public dynamic Data { get; protected set; }

        public XElement ToXML()
        {
            return ToXML(Data);
        }

        protected XElement ToXML(object data)
        {
            if (data is int)
            {
                return new XElement("int", data);
            }
            else if (data is float || data is double)
            {
                return new XElement("float", data);
            }
            else if (data is string)
            {
                return new XElement("string", data);
            }
            else if (data is IDictionary<string, object>)
            {
                return new XElement("dictionary",
                    ((IDictionary<string, object>)data).Select(kvp => new XElement("dictionaryentry",
                        new XAttribute("key", kvp.Key),
                        ToXML(kvp.Value)
                    ))
                );
            }
            else if (data is IEnumerable)
            {
                return new XElement("list",
                    ((IEnumerable)data).Cast<object>().Select(v => ToXML(v))
                );
            }
            else
            {
                return new XElement("object", data);
            }
        }

        protected Dictionary<string, dynamic> ProcessKeyValuePairs(ByteSubArray data, List<string> path)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

            while (true)
            {
                string key = data.GetString();

                if (key == "")
                {
                    return ret;
                }

                List<string> _path = path.ToList();
                _path.Add(key);
                dynamic val = ProcessValue(data, _path);
                ret[key] = val;
            }
        }

        protected int ProcessInt32(ByteSubArray data)
        {
            return data.GetInt32();
        }

        protected List<int> ProcessInt32List(ByteSubArray data)
        {
            List<int> vals = new List<int>();
            int nents = data.GetInt32();
            for (int i = 0; i < nents; i++)
            {
                vals.Add(data.GetInt32());
            }
            return vals;
        }

        protected List<float> ProcessSingleList(ByteSubArray data)
        {
            List<float> vals = new List<float>();
            int nents = data.GetInt32();
            for (int i = 0; i < nents; i++)
            {
                vals.Add(data.GetSingle());
            }
            return vals;
        }

        protected List<string> ProcessStringList(ByteSubArray data)
        {
            List<string> vals = new List<string>();
            int nents = data.GetInt32();
            for (int i = 0; i < nents; i++)
            {
                vals.Add(data.GetString());
            }
            return vals;
        }

        protected List<dynamic> ProcessList(ByteSubArray data, List<string> path)
        {
            List<object> vals = new List<object>();
            int nents = data.GetInt32();
            for (int i = 0; i < nents; i++)
            {
                List<string> _path = path.ToList();
                _path.Add("");
                vals.Add(ProcessValue(data, _path));
            }
            return vals;
        }

        protected dynamic ProcessValue(ByteSubArray data, List<string> path)
        {
            byte type = data.GetByte();
            switch ((char)type)
            {
                case 'T': return ProcessKeyValuePairs(data, path);
                case 'I': return ProcessInt32List(data);
                case 'i': return data.GetInt32();
                case 'F': return ProcessSingleList(data);
                case 'f': return data.GetSingle();
                case 'S': return ProcessStringList(data);
                case 's': return data.GetString();
                case 'M': return ProcessList(data, path);
                case '.': return null;
                default:
                    throw new NotImplementedException(String.Format("Unknown type {0} ({0:X8})", (char)type));
            }
        }

        public RS5Environment(ByteSubArray data)
        {
            data.Position = 0;
            Data = ProcessValue(data, new List<string>());
        }
    }
}
