using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WekaReader.Data
{
    public class DataWarehouse
    {
        int LastID = 0;
        private List<DataRecord> Records;
        private bool IDSupplied = true;
        public string ClassificationAttribute { get; set; }
        public string IDAttribute { get; set; }
        public string[] Attributes { get; set; }
        public string FileName { get; set; }

        public DataRecord[] GetRecords()
        {
            return Records.ToArray();
        }

        public DataRecord GetRecord(int ID)
        {
            if (IDSupplied)
            {
                return (from r in Records where r.ID == ID select r).First();
            }
            else
            {
                return Records[ID];
            }
        }

        public DataRecord GetLatestRecord()
        {
            return Records.Last();
        }

        public void AddRecord(DataRecord Record)
        {
            Record.ID = LastID++;
            Records.Add(Record);
        }

        public bool DeleteRecord(DataRecord Record)
        {
            return (Records.Remove(Record));
        }

        public DataWarehouse()
        {
            Records = new List<DataRecord>();
        }

        public DataWarehouse(string CSVFilePath)
        {
            Records = new List<DataRecord>();
            ReadCSVFile(CSVFilePath);
        }

        public bool ReadCSVFile(string CSVFilePath)
        {
            if(File.Exists(CSVFilePath))
            {
                FileName = Path.GetFileName(CSVFilePath);
                using (StreamReader reader = new StreamReader(CSVFilePath))
                {
                    string line = reader.ReadLine();
                    Attributes = line.Split(',');

                    while (!reader.EndOfStream)
                    {
                        line = reader.ReadLine();
                        string[] lineValues = line.Split(',');
                        DataRecord newRecord = new DataRecord();


                        if (Attributes.First().ToLower() == "id")
                        {
                            IDAttribute = Attributes.First();

                            newRecord.ID = int.Parse(lineValues[0]);
                            if (LastID < newRecord.ID)
                            {
                                LastID = newRecord.ID + 1;
                            }
                            for (int dataIndex = 1; dataIndex < lineValues.Length; ++dataIndex)
                            {
                                newRecord.AddOrUpdateValue(Attributes[dataIndex], lineValues[dataIndex]);
                            }
                        }
                        else
                        {
                            newRecord.ID = LastID++;
                            for (int dataIndex = 0; dataIndex < lineValues.Length; ++dataIndex)
                            {
                                newRecord.AddOrUpdateValue(Attributes[dataIndex], lineValues[dataIndex]);
                            }
                        }
                        Records.Add(newRecord);
                    }
                }
                ClassificationAttribute = Attributes.Last();
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
