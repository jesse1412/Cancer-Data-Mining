using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WekaReader.Classifiers
{
    static class ModelBuilder
    {
        public static AbstractClassifier CreateModel(string Model)
        {
            Type t = GetModelType(Model);
            if(t != null)
            {
                AbstractClassifier classifier = (AbstractClassifier)Activator.CreateInstance(t, Model);
                return classifier;
            }
            return null;
        }

        public static Type GetModelType(string Model)
        {
            AbstractClassifier ModelObject;
            foreach (Type t in GetAllSubclassOf(typeof(AbstractClassifier)))
            {
                ModelObject = ((AbstractClassifier)Activator.CreateInstance(t));
                if (ModelObject.IsThisModelType(Model))
                {
                    return t;
                }
            }
            return null;
        }

        private static IEnumerable<Type> GetAllSubclassOf(Type parent)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in a.GetTypes())
                {
                    if (t.IsSubclassOf(parent))
                    {
                        if (t.ToString() != "WekaReader.Classifiers.Rules.RuleModel"
                            && t.ToString() != "WekaReader.Classifiers.Trees.TreeModel")
                        {
                            yield return t;
                        }
                    }
                }
            }
        }
    }
}
