using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

namespace WekaReader.Data
{
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    public class DataTranslator
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
    {
        public enum EAttributeType
        {
            Boolean,
            Numeric,
            Enumerated
        }



        private Dictionary<string, AttributeInfoBase> attributeInfo;

        /// <summary>
        /// If a custom made translator xml file already exists, use LoadTranslatorFromFile method.
        /// </summary>
        /// <param name="DynamicTranslate"></param>
        public DataTranslator()
        {
            attributeInfo = new Dictionary<string, AttributeInfoBase>();
        }

        public static DataTranslator LoadTranslatorFromFile(string FileToLoad = "translator.trs")
        {
            if (File.Exists(FileToLoad))
            {
                Dictionary<string, AttributeInfoBase> attributeInfos = new Dictionary<string, AttributeInfoBase>();
                using (XmlReader reader = XmlReader.Create(FileToLoad))
                {
                    reader.MoveToContent();
                    reader.MoveToContent();
                    reader.Read();
                    reader.Read();
                    reader.Read();

                    string currentKey = "";

                    while (!reader.EOF)
                    {
                        while (reader.Name != "key" && !reader.EOF)
                        {
                            reader.Read();
                        }
                        if (reader.EOF)
                        {
                            break;
                        }

                        reader.Read();
                        currentKey = reader.ReadContentAsString();
                        reader.MoveToContent();

                        while (reader.Name != "value")
                        {
                            reader.Read();
                        }
                        reader.Read();
                        reader.Read();
                        string type = reader.Name;

                        AttributeInfoBase thisAttributeInfo;
                        switch (type)
                        {
                            case "AttributeInfoBoolean":
                                thisAttributeInfo = new AttributeInfoBoolean(currentKey);
                                break;
                            case "AttributeInfoEnumerated":
                                thisAttributeInfo = new AttributeInfoEnumerated(currentKey, new string[0]);
                                break;
                            case "AttributeInfoNumeric":
                                thisAttributeInfo = new AttributeInfoNumeric(currentKey, 0, 0);
                                break;
                            default:
                                throw new Exception("Incorrect attribute type.");
                        }
                        using (XmlReader subReader = reader.ReadSubtree())
                        {
                            thisAttributeInfo.ReadXml(subReader);
                        }
                        attributeInfos.Add(thisAttributeInfo.AttributeName, thisAttributeInfo);
                    }
                }
                return new DataTranslator() { attributeInfo = attributeInfos };
            }
            else
            {
                throw new Exception("File does not exist: " + FileToLoad);
            }
        }

        public override bool Equals(object obj)
        {
            //((DataTranslator)obj).attributeInfo
            if (attributeInfo.Count == ((DataTranslator)obj).attributeInfo.Count && !attributeInfo.Except(((DataTranslator)obj).attributeInfo).Any())
            {
                return true;
            }
            return false;
        }

        public void SaveTranslatorToFile(string FileToSave = "data.trs")
        {
            if (!File.Exists(FileToSave) || !LoadTranslatorFromFile(FileToSave).Equals(this))
            {
                using (XmlWriter writer = XmlWriter.Create(FileToSave, new XmlWriterSettings { Indent = true }))
                {
                    writer.WriteStartElement("translator");
                    writer.WriteStartElement("attributeInfos");
                    foreach (KeyValuePair<string, AttributeInfoBase> attribute in attributeInfo)
                    {
                        writer.WriteStartElement("attributeInfo");
                        writer.WriteElementString("key", attribute.Key);
                        writer.WriteStartElement("value");
                        attribute.Value.WriteXml(writer);
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.Flush();
                }

                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Green;
                Console.WriteLine("File written: " + FileToSave);
                Console.ResetColor();
            }
        }

        public void CreateOrUpdateTranslator(DataWarehouse InputWH)
        {
            foreach (string attribute in InputWH.Attributes)
            {
                if (attribute != InputWH.ClassificationAttribute && attribute != InputWH.IDAttribute)
                {
                    if (!attributeInfo.ContainsKey(attribute))
                    {
                        string[] uniqueValues = InputWH.GetRecords().Select(r => r.GetAttributeValue(attribute)).Distinct().ToArray();
                        //Check if boolean
                        if (uniqueValues.Count() == 2)
                        {

                            if (uniqueValues.Contains("True") && uniqueValues.Contains("False"))
                            {
                                attributeInfo.Add(attribute, new AttributeInfoBoolean(attribute, "true", "false"));
                            }
                            if (uniqueValues.Contains("true") && uniqueValues.Contains("false"))
                            {
                                attributeInfo.Add(attribute, new AttributeInfoBoolean(attribute));
                            }
                            else if (uniqueValues.Contains("Yes") && uniqueValues.Contains("No"))
                            {
                                attributeInfo.Add(attribute, new AttributeInfoBoolean(attribute, "Yes", "No"));
                            }
                            else if (uniqueValues.Contains("yes") && uniqueValues.Contains("no"))
                            {
                                attributeInfo.Add(attribute, new AttributeInfoBoolean(attribute, "yes", "no"));
                            }
                        }
                        if (!attributeInfo.ContainsKey(attribute))
                        {
                            //Check if all values can be made numeric.
                            if (uniqueValues.Where(v => !decimal.TryParse(v, out decimal dummy)).Count() == 0)
                            {
                                decimal[] uniqueDecimals = Array.ConvertAll(uniqueValues, s => decimal.Parse(s));
                                decimal min = uniqueDecimals.Min();
                                decimal max = uniqueDecimals.Max();
                                attributeInfo.Add(attribute, new AttributeInfoNumeric(attribute, min, max));
                            }
                            else //Must be enum
                            {
                                attributeInfo.Add(attribute, new AttributeInfoEnumerated(attribute, uniqueValues.ToArray()));
                            }
                        }
                    }
                    else
                    {
                        if (attributeInfo[attribute].AttributeType == EAttributeType.Enumerated)
                        {
                            string[] suppliedPrecedence = ((AttributeInfoEnumerated)attributeInfo[attribute]).ValuePrecedence;
                            string[] suppliedPrecedenceSorted = new string[suppliedPrecedence.Length];
                            Array.Copy(suppliedPrecedence, suppliedPrecedenceSorted, suppliedPrecedence.Length);
                            Array.Sort(suppliedPrecedenceSorted);

                            string[] uniqueValues = InputWH.GetRecords().Select(r => r.GetAttributeValue(attribute)).Distinct().ToArray();
                            Array.Sort(uniqueValues);

                            if (!suppliedPrecedenceSorted.SequenceEqual(uniqueValues))
                            {
                                IEnumerable<string> removedValues = suppliedPrecedence.Where(v => !uniqueValues.Contains(v));
                                IEnumerable<string> newValues = uniqueValues.Where(v => !suppliedPrecedence.Contains(v));

                                List<string> newPrecedenceList = new List<string>();
                                newPrecedenceList.AddRange(suppliedPrecedence);
                                newPrecedenceList.AddRange(newValues);
                                foreach (string s in removedValues)
                                {
                                    newPrecedenceList.Remove(s);
                                }

                                string[] newPrecedenceArray = newPrecedenceList.ToArray();

                                if (newPrecedenceArray.Length == 2
                                    && ((newPrecedenceArray.Contains("True") || newPrecedenceArray.Contains("true") || newPrecedenceArray.Contains("yes") || newPrecedenceArray.Contains("Yes"))
                                        && ((newPrecedenceArray.Contains("False") || newPrecedenceArray.Contains("false") || newPrecedenceArray.Contains("No") || newPrecedenceArray.Contains("no")))))
                                {
                                    string[] newPrecedenceArraySorted = newPrecedenceList.ToArray();
                                    Array.Sort(newPrecedenceArraySorted);
                                    string[] yesNoVals = (newPrecedenceArraySorted.Reverse().ToArray());
                                    attributeInfo[attribute] = new AttributeInfoBoolean(attribute, yesNoVals[1], yesNoVals[0]);

                                    Console.ForegroundColor = ConsoleColor.Black;
                                    Console.BackgroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine("[WARNING] boolean/enumaration mis-match, type changed to boolean: ");
                                    Console.WriteLine("\tAttribute: " + attribute);
                                }
                                else
                                {
                                    ((AttributeInfoEnumerated)attributeInfo[attribute]).ValuePrecedence = newPrecedenceArray;

                                    Console.ForegroundColor = ConsoleColor.Black;
                                    Console.BackgroundColor = ConsoleColor.Yellow;
                                    Console.WriteLine("[WARNING] Precedence value mis-match, new enumerated value(s) added/removed: ");
                                    Console.WriteLine("\tAttribute: " + attribute);
                                    Console.Write("\tOld Values/Precedence: ");
                                    foreach (string s in suppliedPrecedence) { Console.Write(s + ','); }
                                    Console.WriteLine();
                                    Console.Write("\tNew Values: ");
                                    foreach (string s in newValues) { Console.Write(s + ','); }
                                    Console.WriteLine();
                                    Console.Write("\tRemoved Values: ");
                                    foreach (string s in removedValues) { Console.Write(s + ','); }
                                    Console.WriteLine();
                                    Console.Write("\tNew Precendence: ");
                                    foreach (string s in newPrecedenceArray) { Console.Write(s + ','); }
                                    Console.WriteLine();

                                    if (newValues.Count() > 0)
                                    {
                                        Console.WriteLine("Consider re-arranging precedence order for this attribute in the translator file (.trs).");
                                    }
                                    Console.ResetColor();
                                }
                                Console.WriteLine();
                            }
                        }
                        else if (attributeInfo[attribute].AttributeType == EAttributeType.Numeric)
                        {
                            string[] uniqueValues = InputWH.GetRecords().Select(r => r.GetAttributeValue(attribute)).Distinct().ToArray();
                            decimal[] uniqueDecimals = Array.ConvertAll(uniqueValues, s => decimal.Parse(s));

                            decimal currentMin = uniqueDecimals.Min();
                            decimal currentMax = uniqueDecimals.Max();

                            if (((AttributeInfoNumeric)attributeInfo[attribute]).Min != currentMin
                                || ((AttributeInfoNumeric)attributeInfo[attribute]).Max != currentMax)
                            {
                                Console.ForegroundColor = ConsoleColor.Black;
                                Console.BackgroundColor = ConsoleColor.Yellow;
                                Console.WriteLine("Min/Max values changed: ");
                                Console.WriteLine("\tOld Min/Max: "
                                    + ((AttributeInfoNumeric)attributeInfo[attribute]).Min
                                    + "/"
                                    + ((AttributeInfoNumeric)attributeInfo[attribute]).Max);
                                Console.WriteLine("\tNew Min/Max: "
                                    + currentMin
                                    + "/"
                                    + currentMax);
                                Console.ResetColor();
                                Console.WriteLine();

                                ((AttributeInfoNumeric)attributeInfo[attribute]).Min = currentMin;
                                ((AttributeInfoNumeric)attributeInfo[attribute]).Max = currentMax;
                            }
                        }
                        else if (attributeInfo[attribute].AttributeType == EAttributeType.Boolean)
                        {
                            if (((AttributeInfoBoolean)attributeInfo[attribute]).ValuePrecedence.Length != 2
                                || !((((AttributeInfoBoolean)attributeInfo[attribute]).ValuePrecedence.Contains("True")
                                    || ((AttributeInfoBoolean)attributeInfo[attribute]).ValuePrecedence.Contains("true")
                                    || ((AttributeInfoBoolean)attributeInfo[attribute]).ValuePrecedence.Contains("yes")
                                    || ((AttributeInfoBoolean)attributeInfo[attribute]).ValuePrecedence.Contains("Yes"))
                                    && ((AttributeInfoBoolean)attributeInfo[attribute]).ValuePrecedence.Contains("False")
                                    || ((AttributeInfoBoolean)attributeInfo[attribute]).ValuePrecedence.Contains("false")
                                    || ((AttributeInfoBoolean)attributeInfo[attribute]).ValuePrecedence.Contains("No")
                                    || ((AttributeInfoBoolean)attributeInfo[attribute]).ValuePrecedence.Contains("no")))
                            {
                                attributeInfo[attribute] = new AttributeInfoEnumerated(
                                    attribute,
                                    ((AttributeInfoBoolean)attributeInfo[attribute]).ValuePrecedence);
                            }
                        }
                    }
                }
            }
        }

        public DataWarehouse TranslateWarehouse(DataWarehouse InputWH)
        {
            CreateOrUpdateTranslator(InputWH);

            DataWarehouse newWarehouse = new DataWarehouse
            {
                Attributes = new List<string>(InputWH.Attributes).ToArray(),
                ClassificationAttribute = InputWH.ClassificationAttribute,
                IDAttribute = InputWH.IDAttribute,
                FileName = InputWH.FileName
            };

            foreach (DataRecord r in InputWH.GetRecords())
            {
                DataRecord newRecord = new DataRecord(r);

                foreach (string attribute in r.GetAttributes())
                {
                    if (attribute != newWarehouse.ClassificationAttribute)
                    {
                        newRecord.AddOrUpdateValue(attribute, attributeInfo[attribute].TranslateValue(r.GetAttributeValue(attribute)));
                    }
                }
                newWarehouse.AddRecord(newRecord);
            }

            return newWarehouse;
        }

        public DataRecord TranslateRecord(DataRecord InputR)
        {
            foreach (string attribute in attributeInfo.Keys)
            {
                InputR.AddOrUpdateValue(attribute, attributeInfo[attribute].TranslateValue(InputR.GetAttributeValue(attribute)));
            }
            return InputR;
        }

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        public abstract class AttributeInfoBase : IXmlSerializable
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        {
            public override bool Equals(object obj)
            {
                if (AttributeName == ((AttributeInfoBase)obj).AttributeName)
                {
                    if (AttributeType == ((AttributeInfoBase)obj).AttributeType)
                    {
                        return true;
                    }
                }
                return false;
            }
            public XmlSchema GetSchema()
            {
                return null;
            }
            public abstract void WriteXml(XmlWriter Writer);
            public abstract void ReadXml(XmlReader Reader);
            public string AttributeName { get; set; }
            public EAttributeType AttributeType { get; set; }
            public abstract string TranslateValue(string StringToTranslate);
            public AttributeInfoBase(string AttributeName, EAttributeType AttributeType)
            {
                this.AttributeName = AttributeName;
                this.AttributeType = AttributeType;
            }
        }

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        public class AttributeInfoNumeric : AttributeInfoBase
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        {
            public override bool Equals(object obj)
            {
                if (Min == ((AttributeInfoNumeric)obj).Min)
                {
                    if (Max == ((AttributeInfoNumeric)obj).Max)
                    {
                        return base.Equals(obj);
                    }
                }
                return false;
            }
            public decimal Min { get; set; }
            public decimal Max { get; set; }
            public override string TranslateValue(string StringToTranslate)
            {
                decimal value = decimal.Parse(StringToTranslate);
                return ((value - Min) / (Max - Min)).ToString();
            }
            public override void WriteXml(XmlWriter Writer)
            {
                Writer.WriteStartElement(this.GetType().Name.ToString());
                Writer.WriteElementString("attributeName", AttributeName);
                Writer.WriteElementString("min", Min.ToString());
                Writer.WriteElementString("max", Max.ToString());
                Writer.WriteEndElement();
            }
            public override void ReadXml(XmlReader Reader)
            {
                while (Reader.Name != "attributeName")
                {
                    Reader.Read();
                }
                Reader.Read();
                AttributeName = Reader.ReadContentAsString();

                while (Reader.Name != "min")
                {
                    Reader.Read();
                }
                Reader.Read();
                Min = decimal.Parse(Reader.ReadContentAsString());

                while (Reader.Name != "max")
                {
                    Reader.Read();
                }
                Reader.Read();
                Max = decimal.Parse(Reader.ReadContentAsString());
            }

            public AttributeInfoNumeric(string AttributeName, decimal Min, decimal Max, EAttributeType AttributeType = EAttributeType.Numeric) :
                base(AttributeName, AttributeType)
            {
                this.Min = Min;
                this.Max = Max;
            }
        }
#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        public class AttributeInfoEnumerated : AttributeInfoBase
#pragma warning restore CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()
        {
            public override bool Equals(object obj)
            {
                if (Enumerable.SequenceEqual(ValuePrecedence, (((AttributeInfoEnumerated)obj).ValuePrecedence)))
                {
                    return base.Equals(obj);
                }
                return false;
            }
            //Order that determines how values are mapped to numeric, between 0-1, from index 0 to n.
            public string[] ValuePrecedence { get; set; }
            public override string TranslateValue(string StringToTranslate)
            {
                int index = Array.IndexOf(ValuePrecedence, StringToTranslate);
                if (ValuePrecedence.Length > 1)
                {
                    return (((decimal)index) / (ValuePrecedence.Length - 1)).ToString();
                }
                else
                {
                    return 0.ToString();
                }
            }
            public override void WriteXml(XmlWriter Writer)
            {
                Writer.WriteStartElement(this.GetType().Name.ToString());
                Writer.WriteElementString("attributeName", AttributeName);
                Writer.WriteStartElement("valuePrecedence");
                foreach (string value in ValuePrecedence)
                {
                    Writer.WriteElementString("value", value);
                }
                Writer.WriteEndElement();
                Writer.WriteEndElement();
            }
            public override void ReadXml(XmlReader Reader)
            {
                while (Reader.Name != "attributeName")
                {
                    Reader.Read();
                }
                Reader.Read();
                AttributeName = Reader.ReadContentAsString();

                List<string> newValuePrecedence = new List<string>();
                while (!Reader.EOF)
                {
                    if (Reader.Name != "value")
                    {
                        Reader.Read();
                    }
                    else
                    {
                        Reader.Read();
                        newValuePrecedence.Add(Reader.ReadContentAsString());
                        Reader.Read();
                    }
                }
                ValuePrecedence = newValuePrecedence.ToArray();
            }
            public AttributeInfoEnumerated(string AttributeName, string[] ValuePrecedence, EAttributeType AttributeType = EAttributeType.Enumerated) :
                base(AttributeName, AttributeType)
            {
                this.ValuePrecedence = ValuePrecedence;
            }
        }
        public class AttributeInfoBoolean : AttributeInfoEnumerated
        {
            public AttributeInfoBoolean(string AttributeName, string TrueBoolString = "true", string FalseBoolString = "false") :
                base(AttributeName, new string[] { FalseBoolString, TrueBoolString }, EAttributeType.Boolean)
            {
            }
        }
    }
}
