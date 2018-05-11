using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WekaReader.Data;

namespace WekaReader.Visualization
{
    class ManhattanDistanceFrequencies : MinkowskiDistanceFrequencies
    {
        public override string[] Visualize(string outputFilePath, DataWarehouse WH, DataRecord Record = null)
        {
            distanceName = "Manhattan";
            return OutputImage(outputFilePath, WH, Record, 1);
        }
    }
}
