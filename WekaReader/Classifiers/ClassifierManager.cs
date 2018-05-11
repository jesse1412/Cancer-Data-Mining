using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using WekaReader.Data;

namespace WekaReader.Classifiers
{
    class ClassifierManager
    {
        private AbstractClassifier[] Models;
        /// <summary>
        /// Used to classify on models simultaneously. 
        /// Multi-threaded handling of classification where appropriate.
        /// </summary>
        /// <param name="Models"></param>
        public ClassifierManager(AbstractClassifier[] Models)
        {
            this.Models = Models;
        }
        /// <summary>
        /// Classify a record using all available models. 
        /// Note, use a single call on a wharehouse of records instead 
        /// of multiple repeated calls to use threading 
        /// optimised for larger datasets.
        /// </summary>
        /// <param name="Record"></param>
        /// <returns></returns>
        public Classification[] ThreadedClassify(DataRecord Record)
        {
            if (Models.Length > 1)
            {
                List<Classification> classifications = new List<Classification>();
                Parallel.ForEach(Models, (m) =>
                {
                    Classification c = m.Classify(Record);
                    c.ModelName = m.Name;
                    c.ClassificationID = Record.ID;
                    classifications.Add(c);
                });
                return classifications.ToArray();
            }
            else
            {
                return Classify(Record);
            }
        }
        public Classification[] ThreadedClassify(DataWarehouse Warehouse)
        {
            ConcurrentQueue<Classification> classifications = new ConcurrentQueue<Classification>();

            Parallel.ForEach(Warehouse.GetRecords(), (r) =>
            {
                foreach (Classification c in Classify(r))
                {
                    classifications.Enqueue(c);
                }
            });

            return classifications.ToArray();
        }
        private Classification[] Classify(DataRecord Record)
        {
            List<Classification> classifications = new List<Classification>();
            foreach (AbstractClassifier m in Models)
            {
                Classification c = m.Classify(Record);
                c.ModelName = m.Name;
                c.ClassificationID = Record.ID;
                classifications.Add(c);
            }
            return classifications.ToArray();
        }
    }
}
