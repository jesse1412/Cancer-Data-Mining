using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using WekaReader.Classifiers;
using WekaReader.Visualization;

namespace WekaReader
{
    class SetUp
    {
        public SetUp()
        {
        }

        /// <summary>
        /// Fixes missing directory in config.xml.
        /// </summary>
        /// <param name="Config"></param>
        private void InvalidModelPathProvided(XmlDocument Config)
        {
            const string defaultDir = "models";

            XmlNode modelDirNode = Config.SelectSingleNode("configuration").SelectSingleNode("settings").SelectSingleNode("modelDirectory");
            XmlNode modelDirNodeVal;
            if (modelDirNode != null)
            {
                modelDirNodeVal = modelDirNode.FirstChild;
                if (modelDirNodeVal == null)
                {
                    modelDirNodeVal = modelDirNode.AppendChild(Config.CreateNode(XmlNodeType.Text, "", null));
                }
            }
            else
            {
                Config.SelectSingleNode("settings").AppendChild(Config.CreateNode(XmlNodeType.Element, "modelDirectory", null)).AppendChild(Config.CreateNode(XmlNodeType.Text, "", null));
                modelDirNode = Config.SelectSingleNode("settings").SelectSingleNode("modelDirectory");
                modelDirNodeVal = modelDirNode.FirstChild;
            }

            if (YesNoPrompt("Do you wish to provide a new model directory?"))
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Please provide a directory: ");
                Console.ResetColor();
                string dir = Console.ReadLine();
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                modelDirNodeVal.Value = dir;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Green;
                Console.WriteLine("Directy successfully added.");
                Console.ResetColor();
            }
            else
            {
                string currentDir = Directory.GetCurrentDirectory();
                string newModelDir = currentDir + "\\" + defaultDir;
                modelDirNodeVal.Value = newModelDir;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Green;
                Console.WriteLine("Default model directory added to config.xml: " + newModelDir);
                Console.ResetColor();
            }
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Please add model files to the new model directory if needed, then press enter to continue.");
            Console.ResetColor();
            Console.ReadLine();
        }

        /// <summary>
        /// Prompts the user to supply a new model directory as well as supply direct model paths.
        /// </summary>
        /// <param name="Config"></param>
        private void PromptChangeModelDirAndAddDirectModelPaths(XmlDocument Config)
        {
            if (YesNoPrompt("Do you wish to supply a different model directory?"))
            {
                string dir;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Please provide a directory: ");
                Console.ResetColor();
                dir = Console.ReadLine();
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                Config.SelectSingleNode("settings").SelectSingleNode("modelDirectory").FirstChild.Value = dir;
            }

            while (YesNoPrompt("Do you wish to supply a new model path directly?"))
            {
                string path;
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.WriteLine("Please provide a path: ");
                Console.ResetColor();
                path = Console.ReadLine();

                if (File.Exists(path))
                {
                    AddModelsToConfig(Config.DocumentElement.SelectSingleNode("models"), path);
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.WriteLine("Model added.");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine("Invalid model file.");
                    Console.ResetColor();
                }
            }
        }

        /// <summary>
        /// Validates model files and config.xml.
        /// </summary>
        /// <param name="configPath"></param>
        /// <returns>List of model paths.</returns>
        public void RunSetup(out List<string> ModelPaths, out Dictionary<string,bool> VisualizationStatuses, string configPath = null)
        {
            ModelPaths = null;
            VisualizationStatuses = null;
            while (true)
            {
                while (!FindConfigPath(ref configPath)) //While can't find config file.
                {
                    CreateConfig(configPath);
                }

                XmlDocument configFile = new XmlDocument();
                configFile.Load(configPath);

                #region Make sure visalizers node exists in config.xml.
                if (!GetVisualizerNodes(configFile, out XmlNode visualizerNodes))
                {
                    configFile.DocumentElement.AppendChild(configFile.CreateNode(XmlNodeType.Element, "visualizers", null));
                    if (!GetVisualizerNodes(configFile, out visualizerNodes))
                    {
                        throw new Exception("Error, \"visualizers\" node could not be added and/or does not exist.");
                    }
                    configFile.Save(configPath);
                }
                #endregion

                VisualizationStatuses = GetVisualizerStatuses(visualizerNodes);
                IEnumerable<Type> VisualizerTypes = AbstractVisualizer.GetAllSubclasses(typeof(AbstractVisualizer));
                Dictionary<string, bool> MissingVisualizerStatuses = new Dictionary<string, bool>();
                foreach (Type t in VisualizerTypes)
                {
                    string typeName = t.ToString();
                    if (!VisualizationStatuses.ContainsKey(typeName))
                    {
                        MissingVisualizerStatuses.Add(typeName, true);
                    }
                }
                if (MissingVisualizerStatuses.Count > 0)
                {
                    AddVisualizersToConfig(visualizerNodes, MissingVisualizerStatuses);
                    configFile.Save(configPath);
                }
                VisualizationStatuses = GetVisualizerStatuses(visualizerNodes);

                string modelDir;
                bool? modelDirFound;

                while ((modelDirFound = GetModelDirectory(configFile, out modelDir)) != true) //While the directory that model files are stored in can't be found.
                {
                    if (modelDir == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.WriteLine("The model directory provided in config.xml is invalid.");
                        Console.ResetColor();
                        InvalidModelPathProvided(configFile);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.WriteLine("No model directory provided in config.xml");
                        Console.ResetColor();
                        InvalidModelPathProvided(configFile);
                    }
                }

                while (GetDataPath(configFile, out string dataPath) != true) //While data can't be found.
                {
                    if (dataPath == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.WriteLine("The data path provided in config.xml is invalid.");
                        Console.WriteLine("Please provide a new data path or place a file in the following path: ");
                        Console.WriteLine("\t" + configFile.SelectSingleNode("configuration").SelectSingleNode("settings").SelectSingleNode("dataPath").FirstChild.Value);
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.WriteLine("No data path provided in config.xml");
                        Console.WriteLine("Please provide a new data path.");
                        Console.ResetColor();
                    }
                    while (YesNoPrompt("Would you like to provide a new data path?"))
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Please provide a data path: ");
                        Console.ResetColor();
                        string response = Console.ReadLine();
                        if (File.Exists(response))
                        {
                            configFile.SelectSingleNode("configuration").SelectSingleNode("settings").SelectSingleNode("dataPath").FirstChild.Value = response;
                            configFile.Save(configPath);
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.BackgroundColor = ConsoleColor.Green;
                            Console.WriteLine("Data path added to config.xml.");
                            Console.ResetColor();
                            break;
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.BackgroundColor = ConsoleColor.Red;
                            Console.WriteLine("Invalid file path.");
                            Console.ResetColor();
                        }
                    }
                }

                #region Make sure models node exists in config.xml.
                if (!GetModelNodes(configFile, out XmlNode modelNodes))
                {
                    configFile.DocumentElement.AppendChild(configFile.CreateNode(XmlNodeType.Element, "models", null));
                    if (!GetModelNodes(configFile, out modelNodes))
                    {
                        throw new Exception("Error, \"models\" node could not be added and/or does not exist.");
                    }
                    configFile.Save(configPath);
                }
                #endregion

                bool firstLoop = true;

                while ((ModelPaths = GetModelPaths(modelNodes)).Count == 0 || firstLoop) //While there are no models in config.xml OR first run of loop.
                {
                    string[] modelFilesInDir = Directory.GetFiles(modelDir);

                    if (modelFilesInDir.Length > 0)
                    {
                        if (!GetModelNodes(configFile, out modelNodes))
                        {
                            configFile.AppendChild(configFile.CreateNode(XmlNodeType.Element, "models", null));
                            if (!GetModelNodes(configFile, out modelNodes))
                            {
                                throw new Exception("Error, \"models\" node could not be added and/or does not exist.");
                            }
                        }
                        AddModelsToConfig(modelNodes, modelFilesInDir);
                        configFile.Save(configPath);
                    }
                    if (!firstLoop)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.WriteLine("Model directory could not be found and no direct model paths are available.");
                        Console.ResetColor();

                        PromptChangeModelDirAndAddDirectModelPaths(configFile);
                        configFile.Save(configPath);

                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Please provide new model files in the following directory if needed: ");
                        Console.WriteLine('"' + configFile.DocumentElement.SelectSingleNode("settings").SelectSingleNode("modelDirectory").FirstChild.Value + '"');
                        Console.ResetColor();
                    }

                    firstLoop = false;
                }

                bool? AreModelPathsValid;

                while ((AreModelPathsValid = CheckModelPathsValid(ref ModelPaths, out List<string> missingModelPaths)) != true)
                {
                    if (AreModelPathsValid == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Some model paths provided in config.xml are invalid.");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.WriteLine("All model paths provided in config.xml are invalid.");
                        Console.ResetColor();
                    }

                    if (YesNoPrompt("Can you provide paths to missing models?"))
                    {
                        for (int pathIndex = 0; pathIndex < missingModelPaths.Count; ++pathIndex)
                        {
                            Console.ForegroundColor = ConsoleColor.Black;
                            Console.BackgroundColor = ConsoleColor.Yellow;
                            Console.WriteLine("Please provide a new model path to replace this missing path, or press return to skip: ");
                            Console.WriteLine("\t\"" + missingModelPaths[pathIndex] + "\"");
                            Console.ResetColor();

                            string response = Console.ReadLine();
                            if (response != "")
                            {
                                if (File.Exists(response))
                                {
                                    Type responseModelType = GetModelTypeFromFile(File.ReadAllText(response));

                                    if (GetModelByModelPath(modelNodes, missingModelPaths[pathIndex], out XmlNode foundNode))
                                    {
                                        if (responseModelType == GetModelTypeFromNode(foundNode))
                                        {
                                            ReplaceModelPath(modelNodes, response, missingModelPaths[pathIndex]);
                                            missingModelPaths.RemoveAt(pathIndex);
                                            --pathIndex;
                                            configFile.Save(configPath);
                                            Console.ForegroundColor = ConsoleColor.Black;
                                            Console.BackgroundColor = ConsoleColor.Green;
                                            Console.WriteLine("Model path replaced.");
                                            Console.ResetColor();
                                        }
                                        else
                                        {
                                            Console.ForegroundColor = ConsoleColor.Black;
                                            Console.BackgroundColor = ConsoleColor.Red;
                                            Console.WriteLine("Model file provided is not the same type as the model being replaced.");
                                            Console.ResetColor();
                                            --pathIndex;
                                        }
                                    }
                                    else
                                    {
                                        throw new Exception("Missing model path could not be found.");
                                    }
                                }
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Black;
                                    Console.BackgroundColor = ConsoleColor.Red;
                                    Console.WriteLine("File provided does not exist.");
                                    Console.ResetColor();
                                    --pathIndex;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Skipped.");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("Model path replacement skipped.");
                    }

                    foreach (string path in missingModelPaths)
                    {
                        ModelPaths.Add(path);
                    }

                    if (CheckModelPathsValid(ref ModelPaths, out missingModelPaths) != true)
                    {
                        if (YesNoPrompt("Do you wish to delete any of the models with missing paths from config.xml?"))
                        {
                            for (int pathIndex = 0; pathIndex < missingModelPaths.Count; ++pathIndex)
                            {
                                if (YesNoPrompt("Do you wish to delete the model with the invalid path \"" + missingModelPaths[pathIndex] + "\" from config.xml?"))
                                {
                                    GetModelByModelPath(modelNodes, missingModelPaths[pathIndex], out XmlNode modelToRemove);
                                    modelNodes.RemoveChild(modelToRemove);
                                    missingModelPaths.RemoveAt(pathIndex);
                                    pathIndex--;
                                    configFile.Save(configPath);

                                    Console.ForegroundColor = ConsoleColor.Black;
                                    Console.BackgroundColor = ConsoleColor.Green;
                                    Console.WriteLine("Model information deleted.");
                                    Console.ResetColor();
                                }
                                else
                                {
                                    Console.WriteLine("Skipped.");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Deletion of invalid models skipped.");
                        }
                    }

                    foreach (string path in missingModelPaths)
                    {
                        ModelPaths.Add(path);
                    }

                    if (CheckModelPathsValid(ref ModelPaths, out missingModelPaths) != true)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.WriteLine("There are still invalid models specified in config.xml.");
                        Console.WriteLine("All invalid models must be resolved to continue.");
                        Console.ResetColor();
                    }

                    foreach (string path in missingModelPaths)
                    {
                        ModelPaths.Add(path);
                    }
                }

                if ((ModelPaths = GetModelPaths(modelNodes)).Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.WriteLine("All model paths valid.");
                    Console.ResetColor();

                    ModelPaths = GetModelPaths(modelNodes);
                    return;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine("No valid models are available in config.xml.");
                    Console.ResetColor();

                    GetModelDirectory(configFile, out string path);
                    if (!YesNoPrompt("Can you supply model files in the current model directory \"" + path + "\"?"))
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.WriteLine("Please contact a software administrator to obtain missing model files.");
                        Console.WriteLine("Press any key to exit");
                        Console.ReadKey();
                        Environment.Exit(0);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("Please place model files in the model directory. Press return once complete.");
                        Console.ResetColor();
                        Console.ReadLine();
                    }
                }
            }
        }

        /// <summary>
        /// Attempt to find config file path and move it into the configPath var.
        /// Checks if the provided path in ConfigPath is valid first.
        /// </summary>
        /// <param name="ConfigPath"></param>
        /// <returns>Whether the config file was found</returns>
        public bool FindConfigPath(ref string ConfigPath)
        {
            if (File.Exists(ConfigPath))
            {
                return true;
            }
            else if (File.Exists("config.xml"))
            {
                ConfigPath = Directory.GetCurrentDirectory() + "\\config.xml";
                return true;
            }
            else
            {
                while (YesNoPrompt("Config file not found, do you wish to provide a path?"))
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Please provide the config.xml file path: ");
                    Console.ResetColor();

                    string providedPath = Console.ReadLine();

                    if (File.Exists(providedPath))
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Green;
                        Console.WriteLine("Config file found.");
                        Console.ResetColor();

                        ConfigPath = providedPath;
                        return true;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Red;
                        Console.WriteLine("Incorrect path.");
                        Console.ResetColor();
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Create config file and return it's path.
        /// </summary>
        /// <returns></returns>
        private void CreateConfig(string ConfigFilePath = null)
        {
            if (ConfigFilePath == null)
            {
                ConfigFilePath = "config.xml";
            }
            using (XmlWriter writer = XmlWriter.Create(ConfigFilePath, new XmlWriterSettings { Indent = true }))
            {
                writer.WriteStartElement("configuration");
                writer.WriteStartElement("settings");
                writer.WriteElementString("outputStandardisedModels", true.ToString());
                writer.WriteElementString("dataPath", Directory.GetCurrentDirectory() + "\\data.csv");
                writer.WriteElementString("translatorPath", Directory.GetCurrentDirectory() + "\\data.trs");
                writer.WriteElementString("modelOutputPath", Directory.GetCurrentDirectory() + "\\output models");
                writer.WriteElementString("modelDirectory", Directory.GetCurrentDirectory() + "\\models");
                writer.WriteElementString("visualizationDirectory", Directory.GetCurrentDirectory() + "\\visualizations");
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.Flush();
            }

            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Green;
            Console.WriteLine("File created: " + ConfigFilePath);
            Console.ResetColor();
        }

        /// <summary>
        /// Propogates a list of model files and their types by searching the given directory.
        /// </summary>
        /// <param name="Directory"></param>
        /// <param name="ModelPaths"></param>
        /// <param name="ModelTypes"></param>
        /// <returns></returns>
        private bool FindModelsInDir(string Directory, out List<string> ModelPaths, out List<Type> ModelTypes)
        {
            if (System.IO.Directory.Exists(Directory))
            {
                string[] paths = System.IO.Directory.GetFiles(Directory);
                ModelPaths = new List<string>();
                ModelTypes = new List<Type>();
                foreach (string s in paths)
                {
                    if (s.Split('/').Last().Split('.').Last() == "model")
                    {
                        string model = File.ReadAllText(s);
                        Type type = GetModelTypeFromFile(model);
                        if (type != null)
                        {
                            ModelPaths.Add(s);
                            ModelTypes.Add(type);
                        }
                    }
                }
                if (ModelPaths.Count > 0)
                {
                    return true;
                }
                else
                {
                    ModelPaths = default(List<string>);
                    ModelTypes = default(List<Type>);
                    return false;
                }
            }
            else
            {
                ModelPaths = default(List<string>);
                ModelTypes = default(List<Type>);
                return false;
            }
        }

        /// <summary>
        /// Get model nodes from the provided config file.
        /// </summary>
        /// <param name="Config"></param>
        /// <param name="ModelNodes"></param>
        /// <returns>True if model nodes found in config file, false otherwise.</returns>
        private bool GetModelNodes(XmlDocument Config, out XmlNode ModelNodes)
        {
            if ((ModelNodes = Config.SelectSingleNode("configuration").SelectSingleNode("models")) != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Return a list of direct model paths.
        /// </summary>
        /// <param name="Directory"></param>
        /// <returns>True if all model</returns>
        private List<string> GetModelPaths(XmlNode ModelNodes)
        {
            List<string> modelPaths = new List<string>();
            if (ModelNodes.ChildNodes.Count > 0)
            {
                foreach (XmlNode child in ModelNodes.ChildNodes)
                {
                    modelPaths.Add(child.SelectSingleNode("path").FirstChild.Value);
                }
            }
            return modelPaths;
        }

        /// <summary>
        /// Get Visualizer nodes from the provided config file.
        /// </summary>
        /// <param name="Config"></param>
        /// <param name="VisualizerNodes"></param>
        /// <returns>True if Visualizer nodes found in config file, false otherwise.</returns>
        private bool GetVisualizerNodes(XmlDocument Config, out XmlNode VisualizerNodes)
        {
            if ((VisualizerNodes = Config.SelectSingleNode("configuration").SelectSingleNode("visualizers")) != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Return a dictionary of whether visualizations are enabled or not.
        /// </summary>
        /// <param name="VisualizerNodes"></param>
        /// <returns></returns>
        private Dictionary<string, bool> GetVisualizerStatuses(XmlNode VisualizerNodes)
        {
            Dictionary<string, bool> visualizerStatuses = new Dictionary<string, bool>();
            if (VisualizerNodes.ChildNodes.Count > 0)
            {
                foreach (XmlNode child in VisualizerNodes.ChildNodes)
                {
                    visualizerStatuses.Add(child.SelectSingleNode("type").FirstChild.Value,
                        Boolean.Parse(child.SelectSingleNode("status").FirstChild.Value));
                }
            }
            return visualizerStatuses;
        }

        /// <summary>
        /// Checks if model files can be found, 
        /// any missing models added to MissingModelPaths.
        /// Missing models removed from ModelPaths
        /// </summary>
        /// <param name="ModelPaths"></param>
        /// <returns>True if all model paths are valid.
        /// Null if only some model paths are valid.
        /// False otherwise.</returns>
        private bool? CheckModelPathsValid(ref List<string> ModelPaths,
            out List<string> MissingModelPaths)
        {
            MissingModelPaths = new List<string>();
            for (int pathIndex = 0; pathIndex < ModelPaths.Count; ++pathIndex)
            {
                if (!File.Exists(ModelPaths[pathIndex]))
                {
                    MissingModelPaths.Add(ModelPaths[pathIndex]);
                    ModelPaths.RemoveAt(pathIndex);
                    --pathIndex;
                }
            }
            if (MissingModelPaths.Count == 0)
            {
                return true;
            }
            else if (ModelPaths.Count == 0 && MissingModelPaths.Count != 0)
            {
                return false;
            }
            else
            {
                return null;
            }
        }

        private void AddVisualizersToConfig(XmlNode VisualizerNodes, Dictionary<string, bool> VisualizerStatuses)
        {
            foreach (XmlNode visualizer in VisualizerNodes.ChildNodes)
            {
                if (VisualizerStatuses.ContainsKey(visualizer.SelectSingleNode("type").FirstChild.Value))
                {
                    VisualizerStatuses.Remove(visualizer.SelectSingleNode("type").FirstChild.Value);
                }
            }

            foreach(KeyValuePair<string, bool> vk in VisualizerStatuses)
            {
                XmlNode visNode = VisualizerNodes.AppendChild(VisualizerNodes.OwnerDocument.CreateNode(XmlNodeType.Element, "visualizer", null));
                XmlNode name = visNode.AppendChild(VisualizerNodes.OwnerDocument.CreateNode(XmlNodeType.Element, "type", null));
                name.AppendChild(VisualizerNodes.OwnerDocument.CreateNode(XmlNodeType.Text, vk.Key, null));
                name.FirstChild.Value = vk.Key;
                XmlNode status = visNode.AppendChild(VisualizerNodes.OwnerDocument.CreateNode(XmlNodeType.Element, "status", null));
                status.AppendChild(VisualizerNodes.OwnerDocument.CreateNode(XmlNodeType.Text, vk.Value.ToString(), null));
                status.FirstChild.Value = vk.Value.ToString();
            }
        }

        private void AddModelsToConfig(XmlNode ModelNodes, string ModelPath)
        {
            string[] modelPath = new string[] { ModelPath };
            AddModelsToConfig(ModelNodes, modelPath);
        }

        private void AddModelsToConfig(XmlNode ModelNodes, string[] ModelPaths)
        {
            List<Type> modelTypes = GetModelTypesFromFiles(ModelPaths.ToArray());
            List<string> existingModelPaths = new List<string>();
            foreach (XmlNode model in ModelNodes)
            {
                existingModelPaths.Add(model.SelectSingleNode("path").FirstChild.Value);
            }
            for (int i = 0; i < ModelPaths.Length; ++i)
            {
                //Checking if the model is already in the config.xml
                bool alreadyExists = false;
                foreach (string s in existingModelPaths)
                {
                    if (s == ModelPaths[i])
                    {
                        alreadyExists = true;
                    }
                }
                if (!alreadyExists)
                {
                    if (modelTypes[i] != null)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.BackgroundColor = ConsoleColor.Green;
                        Console.WriteLine("Adding model path \"" + ModelPaths[i] + "\" to config.xml.");
                        XmlNode modelNode = ModelNodes.AppendChild(ModelNodes.OwnerDocument.CreateNode(XmlNodeType.Element, "model", null));
                        XmlNode path = modelNode.AppendChild(ModelNodes.OwnerDocument.CreateNode(XmlNodeType.Element, "path", null));
                        path.AppendChild(ModelNodes.OwnerDocument.CreateNode(XmlNodeType.Text, ModelPaths[i], null));
                        path.FirstChild.Value = ModelPaths[i];
                        XmlNode type = modelNode.AppendChild(ModelNodes.OwnerDocument.CreateNode(XmlNodeType.Element, "type", null));
                        type.AppendChild(ModelNodes.OwnerDocument.CreateNode(XmlNodeType.Text, modelTypes[i].ToString(), null));
                        type.FirstChild.Value = modelTypes[i].ToString();
                        Console.WriteLine("Model added.");
                        Console.ResetColor();
                    }
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Black;
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.WriteLine("Model path \"" + ModelPaths[i] + "\" found in config.xml.");
                    Console.ResetColor();
                }
            }
        }

        /// <summary>
        /// Prompt the user for a yes/no response.
        /// </summary>
        /// <param name="PromptMessage"></param>
        /// <returns></returns>
        private bool YesNoPrompt(string PromptMessage)
        {
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Yellow;
            Console.WriteLine(PromptMessage + " (y/n): ");
            Console.ResetColor();

            char input = Console.ReadKey().KeyChar;
            Console.WriteLine();
            while (input != 'y' && input != 'n')
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Red;
                Console.WriteLine("Invalid response.");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.WriteLine(PromptMessage + " (y/n): ");
                Console.ResetColor();
                input = Console.ReadKey().KeyChar;
            }

            if (input == 'y')
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Replaces the path of a model with the given OldPath with the NewPath.
        /// </summary>
        /// <param name="ModelNodes"></param>
        /// <param name="NewPath"></param>
        /// <param name="OldPath"></param>
        private void ReplaceModelPath(XmlNode ModelNodes, string NewPath, string OldPath)
        {
            string[] oldPaths = GetModelPaths(ModelNodes).ToArray();
            for (int modelIndex = 0; modelIndex < oldPaths.Length; ++modelIndex)
            {
                if (oldPaths[modelIndex] == OldPath)
                {
                    ModelNodes.ChildNodes[modelIndex].SelectSingleNode("path").FirstChild.Value = NewPath;
                    return;
                }
            }
        }

        /// <summary>
        /// Gets the type of a model from it's file.
        /// </summary>
        /// <param name="ModelFile"></param>
        /// <returns></returns>
        private Type GetModelTypeFromFile(string ModelFile)
        {
            return ModelBuilder.GetModelType(ModelFile);
        }

        /// <summary>
        /// Gets the type of a model from a node.
        /// </summary>
        /// <param name="ModelNode"></param>
        /// <returns></returns>
        private Type GetModelTypeFromNode(XmlNode ModelNode)
        {
            return ModelBuilder.GetModelType(File.ReadAllText(ModelNode.SelectSingleNode("path").FirstChild.Value));
        }

        /// <summary>
        /// Gets a list of model types from the model files provided.
        /// </summary>
        /// <param name="ModelFiles"></param>
        /// <returns></returns>
        private List<Type> GetModelTypesFromFiles(string[] ModelFiles)
        {
            List<Type> types = new List<Type>();
            foreach (string path in ModelFiles)
            {
                if (File.Exists(path))
                {
                    types.Add(GetModelTypeFromFile(File.ReadAllText(path)));
                }
                else
                {
                    types.Add(null);
                }
            }
            return types;
        }

        /// <summary>
        /// Gets the node from ModelsNode that contains the requested model path.
        /// </summary>
        /// <param name="ModelsNode"></param>
        /// <param name="ModelPath"></param>
        /// <param name="FoundModel"></param>
        /// <returns>true if a node is found.</returns>
        private bool GetModelByModelPath(XmlNode ModelsNode, string ModelPath, out XmlNode FoundModel)
        {
            foreach (XmlNode child in ModelsNode)
            {
                if (child.SelectSingleNode("path").FirstChild.Value == ModelPath)
                {
                    FoundModel = child;
                    return true;
                }
            }
            FoundModel = default(XmlNode);
            return true;
        }

        /// <summary>
        /// Attempt to get the model directory from a config xml object.
        /// </summary>
        /// <param name="Config"></param>
        /// <returns>True if valid path found, null if invalid path found, false if no path provided.</returns>
        public bool? GetModelDirectory(XmlDocument Config, out string Path)
        {
            XmlNode pathNode = Config.SelectSingleNode("configuration").SelectSingleNode("settings").SelectSingleNode("modelDirectory").FirstChild;
            if (pathNode != null)
            {
                if (Directory.Exists(pathNode.Value))
                {
                    Path = pathNode.Value;
                    return true;
                }
                else
                {
                    Path = default(string);
                    return null;
                }
            }
            else
            {
                Path = default(string);
                return false;
            }
        }
        /// <summary>
        /// Gets the directory where visualizations are outputted.
        /// </summary>
        /// <param name="Config"></param>
        /// <param name="Path"></param>
        /// <returns></returns>
        public bool? GetVisualizationDirectory(XmlDocument Config, out string Path)
        {
            XmlNode pathNode = Config.SelectSingleNode("configuration").SelectSingleNode("settings").SelectSingleNode("visualizationDirectory").FirstChild;
            if (pathNode != null)
            {
                Path = pathNode.Value;
                if (Directory.Exists(Path))
                {
                    Directory.CreateDirectory(Path);
                }
                return true;

            }
            else
            {
                Path = default(string);
                return false;
            }
        }
        /// <summary>
        /// Attempt to get the data path from a config xml object.
        /// </summary>
        /// <param name="Config"></param>
        /// <returns>True if valid path found, null if invalid path found, false if no path provided.</returns>
        public bool? GetDataPath(XmlDocument Config, out string Path)
        {
            XmlNode pathNode = Config.SelectSingleNode("configuration").SelectSingleNode("settings").SelectSingleNode("dataPath");
            if (pathNode != null)
            {
                if (File.Exists(pathNode.FirstChild.Value))
                {
                    Path = pathNode.FirstChild.Value;
                    return true;
                }
                else if (File.Exists("data.csv"))
                {
                    Path = "data.csv";
                    return true;
                }
                else
                {
                    Path = default(string);
                    return null;
                }
            }
            else if (File.Exists("data.csv"))
            {
                Path = "data.csv";
                return true;
            }
            else
            {
                Path = default(string);
                return false;
            }
        }

        /// <summary>
        /// Attempt to get the translator path from a config xml object.
        /// </summary>
        /// <param name="Config"></param>
        /// <returns>True if valid path found, null if invalid path found, false if no path provided.</returns>
        public bool? GetTranslatorPath(XmlDocument Config, out string Path)
        {
            XmlNode pathNode = Config.SelectSingleNode("configuration").SelectSingleNode("settings").SelectSingleNode("translatorPath");
            if (pathNode != null)
            {
                if (File.Exists(pathNode.FirstChild.Value))
                {
                    Path = pathNode.FirstChild.Value;
                    return true;
                }
                else if (File.Exists("data.trs"))
                {
                    Path = "data.trs";
                    return true;
                }
                else
                {
                    Path = default(string);
                    return null;
                }
            }
            else if (File.Exists("data.trs"))
            {
                Path = "data.trs";
                return true;
            }
            else
            {
                Path = default(string);
                return false;
            }
        }
    }
}
