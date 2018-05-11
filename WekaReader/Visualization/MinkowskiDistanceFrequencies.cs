using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WekaReader.Data;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms.DataVisualization.Charting.Utilities;

namespace WekaReader.Visualization
{
    class MinkowskiDistanceFrequencies : AbstractVisualizer
    {
        protected string distanceName;
        private static double GetDistanceFromZero(DataRecord Record, string IDAttributeName, string ClassificationAttributeName, double Power = 2d)
        {
            double distance = 0;
            DataRecord zero = new DataRecord();
            foreach (string attribute in Record.GetAttributes())
            {
                zero.AddOrUpdateValue(attribute, "0");
            }
            foreach (string attributeName in Record.GetAttributes())
            {
                if (attributeName != ClassificationAttributeName && attributeName != IDAttributeName)
                {
                    distance += Math.Pow(Math.Abs(double.Parse(Record.GetAttributeValue(attributeName)) - double.Parse(zero.GetAttributeValue(attributeName))), Power);
                }
            }
            return Math.Pow(distance, (1 / Power));
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
            return Math.Pow(distance, (1 / Power));
        }
        private static double[] GetDistancesFromZero(DataRecord[] Records, string IDAttributeName, string ClassificationAttributeName, double Power = 2d)
        {
            double[] distances = new double[Records.Length];
            for (int i = 0; i < Records.Count(); ++i)
            {
                distances[i] = GetDistanceFromZero(Records[i], IDAttributeName, ClassificationAttributeName, Power);
            }
            return distances;
        }
        private static double[] GetDistanceFromRecord(DataRecord[] Records, DataRecord Record, string IDAttributeName, string ClassificationAttributeName, double Power = 2d)
        {
            double[] distances = new double[Records.Length];
            for (int i = 0; i < Records.Count(); ++i)
            {
                distances[i] = GetDistanceBetweenRecords(Records[i], Record, IDAttributeName, ClassificationAttributeName, Power);
            }
            return distances;
        }
        private static Dictionary<string, DataRecord[]> SplitDataByClassification(DataWarehouse WH)
        {
            Dictionary<string, DataRecord[]> d = new Dictionary<string, DataRecord[]>();
            string[] classifications = WH.GetRecords().Select(r => r.GetAttributeValue(WH.ClassificationAttribute)).Distinct().ToArray();
            foreach (string classification in classifications)
            {
                d.Add(classification, WH.GetRecords().Where(r => r.GetAttributeValue(WH.ClassificationAttribute) == classification).ToArray());
            }
            return d;
        }
        private static int[] GetFrequenciesInRanges(double[] Values, double Divisor, double Min, double Max)
        {
            int[] freq = new int[(int)((Max - Min) / Divisor)+1];
            for (int i = 0; i < Values.Length; ++i)
            {
                int test = (int)((Values[i] - Min) / Divisor);
                ++freq[(int)((Values[i] - Min) / Divisor)];
            }
            return freq;
        }
        private static Dictionary<string, int[]> GetFrequenciesInRangesByClassifiers(Dictionary<string, double[]> Values, double Divisor, double Min, double Max)
        {
            Dictionary<string, int[]> frequenciesByClassification = new Dictionary<string, int[]>();
            foreach (KeyValuePair<string, double[]> kv in Values)
            {
                frequenciesByClassification.Add(kv.Key, GetFrequenciesInRanges(kv.Value, Divisor, Min, Max));
            }
            return frequenciesByClassification;
        }
        public override string[] Visualize(string outputFilePath, DataWarehouse WH, DataRecord Record = null)
        {
            return new string[0];
        }
        protected virtual string[] OutputImage(string outputFilePath, DataWarehouse WH, DataRecord Record, double power = 2)
        {
            Dictionary<string, DataRecord[]> recordsSplitByClassification = SplitDataByClassification(WH);
            Dictionary<string, double[]> distancesSplitByClassification = new Dictionary<string, double[]>();
            double min = double.MaxValue;
            double max = double.MinValue;
            foreach (KeyValuePair<string, DataRecord[]> kv in recordsSplitByClassification)
            {
                distancesSplitByClassification[kv.Key] = GetDistanceFromRecord(kv.Value, Record, WH.IDAttribute, WH.ClassificationAttribute);
                Array.Sort(distancesSplitByClassification[kv.Key]);
                if (distancesSplitByClassification[kv.Key].Max() > max)
                {
                    max = distancesSplitByClassification[kv.Key].Max();
                }
                if (distancesSplitByClassification[kv.Key].Min() < min)
                {
                    min = distancesSplitByClassification[kv.Key].Min();
                }
            }
            const int divisorCount = 10;
            double divisor = (max - min) / divisorCount;
            double[] ranges = new double[divisorCount+1];
            for (int i = 0; i < divisorCount + 1; ++i)
            {
                ranges[i] = min + i * divisor;
            }
            Dictionary<string, int[]> frequenciesByClassification = GetFrequenciesInRangesByClassifiers(distancesSplitByClassification, divisor, min, max);
            List<string> outputPaths = new List<string>();
            foreach (KeyValuePair<string, int[]> kv in frequenciesByClassification)
            {
                //value is y values, ranges is x values
                Chart chart = new Chart();
                chart.Titles.Add("Frequency of " + distanceName + " distances from " + kv.Key + " classified entities");
                Series serie = new Series
                {
                    Name = "serie"
                };
                serie.ChartType = SeriesChartType.Column;
                serie.BorderWidth = 1;
                serie.BorderDashStyle = ChartDashStyle.Solid;
                chart.Series.Add(serie);
                chart.Series["serie"].Points.DataBindXY(ranges, kv.Value);
                ChartArea chartArea = new ChartArea();
                chartArea.AxisY.Title = "Frequency";
                chartArea.AxisX.Title = "Distance range where x <= distance < next x";
                chartArea.AxisX.Minimum = min;
                chartArea.AxisX.Maximum = max;
                chartArea.AxisX.Interval = divisor;
                chart.ChartAreas.Add(chartArea);
                chart.SaveImage(outputFilePath + "_" + kv.Key + ".png", ChartImageFormat.Png);
                outputPaths.Add(outputFilePath + "_" + kv.Key + ".png");
            }
            return outputPaths.ToArray();
        }
    }
}
