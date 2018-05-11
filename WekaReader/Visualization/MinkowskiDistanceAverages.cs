using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WekaReader.Data;
using System.Collections.Concurrent;
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;

namespace WekaReader.Visualization
{
    public class MinkowskiDistanceAverages : AbstractVisualizer
    {
        protected string distanceName = "";

        private static Dictionary<string, DataRecord> GetAverageClassValues(DataWarehouse WH, DataTranslator Trs)
        {
            WH = Trs.TranslateWarehouse(WH);
            return GetAverageClassValues(WH);
        }

        private static Dictionary<string, DataRecord> GetAverageClassValues(DataWarehouse WH)
        {
            DataRecord[] records = WH.GetRecords();
            string[] classifications = records.Select(r => r.GetAttributeValue(WH.ClassificationAttribute)).Distinct().ToArray();

            ConcurrentDictionary<string, DataRecord> averageDataRecords = new ConcurrentDictionary<string, DataRecord>();

            Parallel.ForEach(classifications, (classification) =>
            {
                DataRecord averageRecord = new DataRecord();

                IEnumerable<DataRecord> targetRecords = records.Where(r => r.GetAttributeValue(WH.ClassificationAttribute) == classification);

                foreach (string attribute in WH.Attributes)
                {
                    if (attribute != WH.ClassificationAttribute && attribute != WH.IDAttribute)
                    {
                        decimal total = 0;
                        foreach (DataRecord r in targetRecords)
                        {
                            total += decimal.Parse(r.GetAttributeValue(attribute));
                        }
                        averageRecord.AddOrUpdateValue(attribute, (total / targetRecords.Count()).ToString());
                    }
                }
                averageDataRecords.TryAdd(classification, averageRecord);
            });
            return new Dictionary<string, DataRecord>(averageDataRecords);
        }
        private static double GetDistanceBetweenRecords(DataRecord LHS, DataRecord RHS, string IDAttributeName, string ClassificationAttributeName, double Power = 2d)
        {
            double distance = 0;
            foreach (string attributeName in LHS.GetAttributes())
            {
                if (attributeName != ClassificationAttributeName && attributeName != IDAttributeName)
                {
                    distance += Math.Pow(Math.Abs(double.Parse(LHS.GetAttributeValue(attributeName)) - double.Parse(RHS.GetAttributeValue(attributeName))), Power);
                }
            }
            return Math.Pow(distance, (1/ Power));
        }
        public override string[] Visualize(string outputFilePath, DataWarehouse WH, DataRecord Record = null)
        {
            return new string[0];
        }

        protected virtual string[] OutputImage(string outputFilePath, DataWarehouse WH, DataRecord Record = null, double power = 2)
        {
            DataRecord[] records = WH.GetRecords();
            Dictionary<string, DataRecord> averageClassValues = GetAverageClassValues(WH);
            string[] targetAttributeValues = averageClassValues.Keys.ToArray();
            double[] distancesFromAverageClasses = new double[targetAttributeValues.Length];
            int count = 0;
            foreach (KeyValuePair<string, DataRecord> kv in averageClassValues)
            {
                distancesFromAverageClasses[count++] = GetDistanceBetweenRecords(Record, kv.Value, WH.IDAttribute, WH.ClassificationAttribute, power);
            }
            Chart chart = new Chart();
            chart.Titles.Add(distanceName + " distance from an average entity of each classification");
            Series serie = new Series
            {
                Name = "serie"
            };
            chart.Series.Add(serie);
            chart.Series["serie"].Points.DataBindXY(targetAttributeValues, distancesFromAverageClasses);
            chart.Series["serie"].ChartType = SeriesChartType.Radar;
            chart.Series["serie"]["RadarDrawingStyle"] = "Area";
            chart.Series["serie"]["AreaDrawingStyle"] = "Polygon";
            chart.Series["serie"]["CircularLabelsStyle"] = "Horizontal";
            ChartArea area = new ChartArea
            {
                Name = "area"
            };
            chart.ChartAreas.Add(area);
            chart.SaveImage(outputFilePath + ".png", ChartImageFormat.Png);
            return new string[] { outputFilePath + ".png" };
        }
    }
}
