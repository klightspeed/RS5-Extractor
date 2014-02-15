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
            if (data == null)
            {
                return new XElement("null");
            }
            else if (data is int)
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

        protected Dictionary<string, dynamic> ProcessKeyValuePairs(SubStream data, List<string> path)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

            while (true)
            {
                string key = data.ReadString();

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

        protected List<object> ProcessRType(SubStream data)
        {
            List<object> vals = new List<object>();
            int nents = data.ReadInt32();
            for (int i = 0; i < nents; i++)
            {
                int v = data.ReadByte();
                if (v != 0)
                {
                    throw new InvalidOperationException(String.Format("I don't know what the data at position {0} is.", data.Position - 1));
                }

                vals.Add(null);
            }

            return vals;
        }
        
        protected List<int> ProcessInt32List(SubStream data)
        {
            List<int> vals = new List<int>();
            int nents = data.ReadInt32();
            for (int i = 0; i < nents; i++)
            {
                vals.Add(data.ReadInt32());
            }
            return vals;
        }

        protected List<float> ProcessSingleList(SubStream data)
        {
            List<float> vals = new List<float>();
            int nents = data.ReadInt32();
            for (int i = 0; i < nents; i++)
            {
                vals.Add(data.ReadSingle());
            }
            return vals;
        }

        protected List<string> ProcessStringList(SubStream data)
        {
            List<string> vals = new List<string>();
            int nents = data.ReadInt32();
            for (int i = 0; i < nents; i++)
            {
                vals.Add(data.ReadString());
            }
            return vals;
        }

        protected List<dynamic> ProcessList(SubStream data, List<string> path)
        {
            List<object> vals = new List<object>();
            int nents = data.ReadInt32();
            for (int i = 0; i < nents; i++)
            {
                List<string> _path = path.ToList();
                _path.Add("");
                vals.Add(ProcessValue(data, _path));
            }
            return vals;
        }

        protected dynamic ProcessValue(SubStream data, List<string> path)
        {
            int type = data.ReadByte();
            switch ((char)type)
            {
                case 'T': return ProcessKeyValuePairs(data, path);
                case 'I': return ProcessInt32List(data);
                case 'i': return data.ReadInt32();
                case 'F': return ProcessSingleList(data);
                case 'f': return data.ReadSingle();
                case 'S': return ProcessStringList(data);
                case 's': return data.ReadString();
                case 'M': return ProcessList(data, path);
                case '.': return null;
                case 'R': return ProcessRType(data);
                default:
                    throw new NotImplementedException(String.Format("Unknown type {0} ({0:X8}) at position {1}", (char)type, data.Position));
            }
        }

        public RS5Environment(SubStream data)
        {
            data.Position = 0;
            Data = ProcessValue(data, new List<string>());
        }
    }
}
