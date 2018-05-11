using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace WekaReader.Classifiers.Trees
{
    class J48 : TreeModel
    {
        public J48()
        {
        }
        public J48(string WekaOutput)
            : base(WekaOutput){}
        protected override List<string> ReadWekaOutput(string Input)
        {
            string[] SplitInput = Input.Replace("\r", String.Empty).Replace("\t", String.Empty).Split('\n');
            List<string> TreeText = new List<string>();
            int line = 0;

            Regex TreeStart = new Regex("J48 (pruned|unpruned) tree");

            while (!TreeStart.IsMatch(SplitInput[line]))
            {
                ++line;
            }
            line += 3;

            while(SplitInput[line] != String.Empty)
            {
                TreeText.Add(SplitInput[line]);
                ++line;
            }

            if(TreeText.Count >= 0)
            {
                return TreeText;
            }
            else
            {
                throw new Exception("No tree found.");
            }
        }
        public override bool IsThisModelType(string Model)
        {
            if (Model.Split('\n').Length > 1)
            {
                return Model.Split('\n')[2].Split(' ')[0] == "Scheme:weka.classifiers.trees.J48";
            }
            else
            {
                return false;
            }
        }
    }
}
