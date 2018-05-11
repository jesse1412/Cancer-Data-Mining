using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WekaReader.Data;

namespace WekaReader.Visualization
{
    public abstract class AbstractVisualizer
    {
        public static IEnumerable<Type> GetAllSubclasses(Type parent)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var t in a.GetTypes())
                {
                    if (t.IsSubclassOf(parent))
                    {
                        yield return t;
                    }
                }
            }
        }
        public abstract string[] Visualize(string outputFilePath, DataWarehouse WH, DataRecord Record = null);
    }
}
