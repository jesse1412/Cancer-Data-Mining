using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WekaReader.Data;

namespace WekaReader.Visualization
{
    class EuclidianDistanceAverages : MinkowskiDistanceAverages
    {
        public override string[] Visualize(string outputFilePath, DataWarehouse WH, DataRecord Record = null)
        {
            distanceName = "Euclidian";
            return OutputImage(outputFilePath, WH, Record, 2);
        }
    }
}
