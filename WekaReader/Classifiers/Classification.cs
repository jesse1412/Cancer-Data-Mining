using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WekaReader.Classifiers
{
    public class Classification
    {
        private List<string> classifications;
        public string[] Classifications
        {
            get
            {
                return classifications.ToArray();
            }
            protected set
            {
                this.classifications = new List<string>(value);
            }
        }
        public string ModelName { get; set; }
        public int ClassificationID { get; set; }
        private List<string> rules;
        public string[] Rules
        {
            get
            {
                return rules.ToArray();
            }
            protected set
            {
                this.rules = new List<string>(value);
            }
        }
        public Classification(string[] Classifications, string[] Rules)
        {
            this.Classifications = Classifications;
            this.Rules = Rules;
        }
        public Classification()
        {
            classifications = new List<string>();
            rules = new List<string>();
        }
        public void AddMatch(string Classification, string Rule)
        {
            classifications.Add(Classification);
            rules.Add(Rule);
        }
        public override string ToString()
        {
            string returnString = "(ID - "+ ClassificationID + ") " + ModelName + " CLASSIFICATION(s): [";
            foreach (string s in classifications)
            {
                returnString += s + ", ";
            }
            returnString = returnString.Substring(0, returnString.Length - 2);
            returnString += "]:";

            for (int i = 0; i < classifications.Count; ++i)
            {
                returnString += "\n\t" + rules[i];
            }
            return returnString;
        }
    }
}
