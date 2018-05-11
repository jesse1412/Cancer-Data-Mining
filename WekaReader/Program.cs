using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using WekaReader.Classifiers.Trees;
using WekaReader.Classifiers.Rules;
using System.Xml;
using WekaReader.Classifiers;
using System.Diagnostics;
using WekaReader.Data;
using WekaReader.Visualization;
using System.Collections.Concurrent;

namespace WekaReader
{
    class Program
    {
        static DataWarehouse warehouse;
        static ClassifierManager cm;
        static DataTranslator translator;
        static List<AbstractVisualizer> Visualizers;
        static string VisualizerOutputDir;

        static void GetClassifiersFromPaths(List<string> ModelPaths,
            out List<AbstractClassifier> Models)
        {
            Models = new List<AbstractClassifier>();

            foreach (string path in ModelPaths)
            {
                AbstractClassifier classifier = ModelBuilder.CreateModel(File.ReadAllText(path));
                if (classifier != null)
                {
                    classifier.Name = path.Split('\\').Last().Split('.').First();
                    Models.Add(classifier);
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Invalid model file found: \"" + path + "\"");
                    Console.WriteLine("Skipping model creation.");
                    Console.ResetColor();
                }
            }
        }
        static void GetVisualizers(Dictionary<string, bool> VisualizationStatuses, out List<AbstractVisualizer> Visualizations)
        {
            Visualizations = new List<AbstractVisualizer>();
            foreach (KeyValuePair<string, bool> kv in VisualizationStatuses)
            {
                if (kv.Value)
                {
                    Visualizations.Add((AbstractVisualizer)Activator.CreateInstance(Type.GetType(kv.Key)));
                }
            }
        }
        static void OutputStandardisedModels(XmlDocument Config, List<AbstractClassifier> Models)
        {
            string outputModelDir = Config.DocumentElement.SelectSingleNode("settings").SelectSingleNode("modelOutputPath").FirstChild.Value;

            for (int modelIndex = 0; modelIndex < Models.Count; ++modelIndex)
            {
                string modelFileName = Models[modelIndex].Name;
                string outputFileName = modelFileName + "-std.model";
                string outputPath = outputModelDir + "\\" + outputFileName;
                if (!Directory.Exists(outputModelDir))
                {
                    Directory.CreateDirectory(outputModelDir);
                }
                using (StreamWriter writer = new StreamWriter(outputPath))
                {
                    writer.Write(Models[modelIndex].GetStringOfRules());
                }
            }
        }
        static void WriteAllClassifications()
        {
            foreach (Classification c in cm.ThreadedClassify(warehouse))
            {
                Console.WriteLine(c.ModelName + ", user " + c.ClassificationID + ": ");
                foreach (string s in c.Classifications)
                {
                    Console.WriteLine('\t' + s);
                }
            }
        }
        static void PropogateDataAndModels()
        {
            SetUp start = new SetUp();
            start.RunSetup(out List<string> modelPaths, out Dictionary<string, bool> visualizationStatuses);

            GetClassifiersFromPaths(modelPaths,
                out List<AbstractClassifier> models);

            GetVisualizers(visualizationStatuses, out Visualizers);

            string configPath = null;
            XmlDocument config;
            bool outputStandardModels;

            if (start.FindConfigPath(ref configPath))
            {
                config = new XmlDocument();
                config.Load(configPath);
                outputStandardModels = Convert.ToBoolean(config.DocumentElement.SelectSingleNode("settings").SelectSingleNode("outputStandardisedModels").FirstChild.Value);
            }
            else
            {
                throw new Exception("Config path not found.");
            }

            if (outputStandardModels)
            {
                OutputStandardisedModels(config, models);
            }

            if (start.GetDataPath(config, out string dataPath) == true)
            {
                warehouse = new DataWarehouse(dataPath);
            }
            else
            {
                throw new Exception("No data found.");
            }

            if (start.GetTranslatorPath(config, out string translatorPath) == true)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Green;
                Console.WriteLine("Translator file found: " + translatorPath);
                Console.ResetColor();
                translator = DataTranslator.LoadTranslatorFromFile(translatorPath);
            }
            else
            {
                translator = new DataTranslator();
            }

            start.GetVisualizationDirectory(config, out VisualizerOutputDir);

            cm = new ClassifierManager(models.ToArray());
        }
        static void UserInteraction()
        {
            bool validIDSupplied = false;
            int targetID = -1;

            do
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Please enter an ID, or press return to classify latest entry.");
                Console.ResetColor();

                string input = Console.ReadLine();
                if (input == "")
                {
                    break;
                }
                else
                {
                    if (int.TryParse(input, out targetID))
                    {
                        validIDSupplied = targetID < warehouse.GetRecords().Count() && targetID >= 0;
                    }
                    if (!validIDSupplied)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid ID supplied.");
                        Console.ResetColor();
                    }
                }
            }
            while (!validIDSupplied);

            Console.Clear();

            DataWarehouse translatedWarehouse = translator.TranslateWarehouse(warehouse);
            translator.SaveTranslatorToFile();

            DataRecord targetRecord = null;
            if (targetID > -1)
            {
                targetRecord = warehouse.GetRecord(targetID);
            }
            else
            {
                targetRecord = warehouse.GetLatestRecord();
            }

            List<string> classificationStrings = new List<string>();
            foreach (Classification c in cm.ThreadedClassify(targetRecord))
            {
                string s = c.ToString();
                classificationStrings.Add(s);
                Console.WriteLine(s);
                Console.WriteLine();
            }

            DataRecord targetRecordTranslated = translator.TranslateRecord(targetRecord);
            ConcurrentBag<string> visualizationBag = new ConcurrentBag<string>();

            if (Visualizers.Count > 0)
            {
                if (!Directory.Exists(VisualizerOutputDir + "\\" + translatedWarehouse.FileName + "\\" + targetRecord.ID))
                {
                    Directory.CreateDirectory(VisualizerOutputDir + "\\" + translatedWarehouse.FileName + "\\" + targetRecord.ID);
                }
                Parallel.ForEach(Visualizers, (v) =>
                {
                    string[] paths = v.Visualize(VisualizerOutputDir + "\\" + translatedWarehouse.FileName + "\\" + targetRecord.ID + "\\" + v.GetType().Name.ToString(),
                       translatedWarehouse,
                       targetRecordTranslated);

                    foreach (string path in paths)
                    {
                        visualizationBag.Add(path);
                    }
                });
            }

            ReportGenerator.GenerateReport(VisualizerOutputDir + "\\" + translatedWarehouse.FileName + "\\" + targetRecord.ID + "\\Report.html",
                targetRecord.ID,
                classificationStrings.ToArray(),
                visualizationBag.ToArray());

            Process.Start(VisualizerOutputDir + "\\" + translatedWarehouse.FileName + "\\" + targetRecord.ID + "\\Report.html");
        }

        static void Main(string[] args)
        {
            PropogateDataAndModels();
            while (true)
            {
                UserInteraction();
            }
        }
    }
}
