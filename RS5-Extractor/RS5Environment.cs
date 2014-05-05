using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Dynamic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.IO;

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
            else if (data is byte[])
            {
                return new XElement("binary", Convert.ToBase64String((byte[])data));
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

        protected void WriteKeyValuePairs(SubStream outstream, IDictionary data)
        {
            foreach (DictionaryEntry de in data)
            {
                outstream.WriteString(de.Key.ToString());
                WriteValue(outstream, de.Value);
            }

            outstream.WriteByte(0);
        }

        protected byte[] ProcessRawBinary(SubStream data)
        {
            List<object> vals = new List<object>();
            int nents = data.ReadInt32();
            byte[] rawdata = data.ReadBytes(nents);
            return rawdata;
        }

        protected void WriteRawBinary(SubStream outstream, byte[] data)
        {
            outstream.WriteInt32(data.Length);
            outstream.WriteBytes(data.Length, data);
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

        protected void WriteInt32List(SubStream outstream, List<int> data)
        {
            outstream.WriteInt32(data.Count);
            foreach (int val in data)
            {
                outstream.WriteInt32(val);
            }
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

        protected void WriteSingleList(SubStream outstream, List<float> data)
        {
            outstream.WriteInt32(data.Count);
            foreach (float val in data)
            {
                outstream.WriteSingle(val);
            }
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

        protected void WriteStringList(SubStream outstream, List<string> data)
        {
            outstream.WriteInt32(data.Count);
            foreach (string val in data)
            {
                outstream.WriteString(val);
            }
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

        protected void WriteList(SubStream outstream, IList data)
        {
            outstream.WriteInt32(data.Count);
            foreach (object val in data)
            {
                WriteValue(outstream, val);
            }
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
                case 'R': return ProcessRawBinary(data);
                default:
                    throw new NotImplementedException(String.Format("Unknown type {0} ({0:X8}) at position {1}", (char)type, data.Position));
            }
        }

        protected void WriteValue(SubStream outstream, object data)
        {
            if (data == null)
            {
                outstream.WriteByte((byte)'.');
            }
            else if (data is string)
            {
                outstream.WriteByte((byte)'s');
                outstream.WriteString((string)data);
            }
            else if (data is float)
            {
                outstream.WriteByte((byte)'f');
                outstream.WriteSingle((float)data);
            }
            else if (data is int)
            {
                outstream.WriteByte((byte)'i');
                outstream.WriteInt32((int)data);
            }
            else if (data is byte[])
            {
                outstream.WriteByte((byte)'R');
                WriteRawBinary(outstream, (byte[])data);
            }
            else if (data is List<string>)
            {
                outstream.WriteByte((byte)'S');
                WriteStringList(outstream, (List<string>)data);
            }
            else if (data is List<float>)
            {
                outstream.WriteByte((byte)'F');
                WriteSingleList(outstream, (List<float>)data);
            }
            else if (data is List<int>)
            {
                outstream.WriteByte((byte)'I');
                WriteInt32List(outstream, (List<int>)data);
            }
            else if (data is IList)
            {
                outstream.WriteByte((byte)'M');
                WriteList(outstream, (IList)data);
            }
            else if (data is IDictionary)
            {
                outstream.WriteByte((byte)'T');
            }
            else
            {
                throw new InvalidOperationException(String.Format("Unable to write value of type {0}", data.GetType().ToString()));
            }
        }

        public RS5Environment(SubStream data)
        {
            data.Position = 0;
            Data = ProcessValue(data, new List<string>());
        }

        public void WriteTo(SubStream stream)
        {
            WriteValue(stream, this.Data);
        }

        public void WriteTo(Stream stream)
        {
            using (SubStream tmpstream = new SubStream(new MemoryStream()))
            {
                WriteTo(tmpstream);
                tmpstream.Seek(0, SeekOrigin.Begin);
                tmpstream.CopyTo(stream);
            }
        }
    }
}
