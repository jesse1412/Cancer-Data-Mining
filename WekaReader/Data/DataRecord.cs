using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WekaReader.Data
{
    public class DataRecord
    {
        private Dictionary<string, string> Values;
        public int ID { get; set; }
        public void AddOrUpdateValue(string AttributeName, string Value)
        {
            if(AttributeName.ToLower() == "id")
            {
                if(int.TryParse(Value, out int newID))
                {
                    ID = newID;
                }
            }
            if (Values.ContainsKey(AttributeName))
            {
                Values[AttributeName] = Value;
            }
            else
            {
                Values.Add(AttributeName, Value);
            }
        }
        public string GetAttributeValue(string Attribute)
        {
            return Values[Attribute];
        }

        public string[] GetAttributes()
        {
            return Values.Keys.ToArray();
        }

        public DataRecord()
        {
            Values = new Dictionary<string, string>();
        }
        public DataRecord(DataRecord r)
        {
            Values = new Dictionary<string, string>(r.Values);
            ID = r.ID;
        }
    }
}
