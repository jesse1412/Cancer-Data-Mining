using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using WekaReader.Classifiers;
using WekaReader.Data;

namespace WekaReader.Classifiers.Trees
{

    public abstract class TreeModel : AbstractClassifier
    {
        private TreeNode root;

        string DefaultClassification;

        /// <summary>
        /// Returns lines defining the tree from the weka output.
        /// </summary>
        /// <param name="Input">Weka output for tree</param>
        /// <returns></returns>
        protected abstract List<string> ReadWekaOutput(string Input);

        public TreeModel(string WekaOutput, string DefaultClassification = "Unclassified")
        {
            this.DefaultClassification = DefaultClassification;
            root = new TreeNode(ReadWekaOutput(WekaOutput), null);
        }

        protected TreeModel()
        {
        }

        public override string GetStringOfRules()
        {
            return "STANDARDISED RULE FORMAT\r\n" + root.GetStringOfRules() + "DEFAULT: " + DefaultClassification + "\r\n";
        }

        public override Classification Classify(DataRecord Record)
        {
            return root.Classify(Record);
        }
    }

    public class TreeNode
    {
        private Dictionary<int, TreeNode> Nodes;
        private List<string> RHSValues;
        private List<string> LHSAttributes;
        private List<string> Comparators;
        private Dictionary<int, string> Classifications;
        private TreeNode Parent;

        /// <summary>
        /// Create new branches on current node and continue tree propogation.
        /// </summary>
        /// <param name="FurtherInput"></param>
        private void ParseTreeSection(List<string> FurtherInput)
        {
            List<string> NextInput = new List<string>();
            string[] splitLine;
            int pathCount = 0;

            for (int lineNo = 0; lineNo < FurtherInput.Count; lineNo++)
            {
                while (lineNo < FurtherInput.Count && FurtherInput[lineNo][0] == '|')
                {
                    if (FurtherInput[lineNo].Length > 4)
                    {
                        NextInput.Add(FurtherInput[lineNo].Substring(4));
                    }
                    ++lineNo;
                }

                if (lineNo != 0 && NextInput.Count > 0)
                {
                    Nodes.Add(key: pathCount++, value: new TreeNode(NextInput, this));
                    NextInput.Clear();
                }
                if (lineNo < FurtherInput.Count)
                {
                    splitLine = FurtherInput[lineNo].Split(' ');
                    LHSAttributes.Add(splitLine[0]);
                    Comparators.Add(splitLine[1]);
                    if (splitLine.Count() > 3)
                    {
                        string rhs = splitLine[2];
                        rhs = rhs.Substring(0, rhs.Length - 1);
                        RHSValues.Add(rhs);
                        Classifications.Add(key: pathCount++, value: splitLine[3]);
                    }
                    else
                    {
                        RHSValues.Add(splitLine[2]);
                    }
                }
            }
        }

        /// <summary>
        /// Write out the tree in rule form.
        /// </summary>
        public virtual string GetStringOfRules()
        {

            StringBuilder AllRules = new StringBuilder();
            string currentRule; //Used to build up a rule. Fed into WriteBranch method.

            //For every child of this node.
            for (int i = 0; i < LHSAttributes.Count; ++i)
            {
                currentRule = ""; //Clear the current rule (empty at the start of each rule).
                currentRule += "If ( [ " + LHSAttributes[i] + " " + Comparators[i] + " " + RHSValues[i];
                //If this node is a leaf/contains a classifier value.
                if (Classifications.ContainsKey(i))
                {
                    currentRule += " ] ) --> " + Classifications[i];
                    AllRules.AppendLine(currentRule);
                }
                else //If the rule isn't fully built.
                {
                    currentRule += " ] & [ ";
                    //Call the recursive version of this method that will fully evaluate each path of the next node.
                    Nodes[i].WriteBranch(currentRule, ref AllRules);

                    /* Tracks how many nodes have been written.
                     * Used because this node may contain both leaves and stems.
                     * If this branch contains a leaf then one node print ends up being skipped,
                     * i increases without printing the ith node. This means that on
                     * the next non-leaf iteration, Nodes[i] could be out of range as there are
                     * more "rules" to evaluate than nodes to print. Solution: Track nodes printed.*/
                }
            }
            return AllRules.ToString();

        }

        /// <summary>
        /// Varient of write rules than can operate recursively down the tree.
        /// </summary>
        /// <param name="CurrentRule"></param>
        private void WriteBranch(string CurrentRule, ref StringBuilder AllRules)
        {
            /*Every full rule that stems from this node inherits the same rules from its ancestors,
             * hence we need to remember the rules that have been passed down.*/
            string OldRule = CurrentRule;

            //For every child of this node.
            for (int i = 0; i < LHSAttributes.Count; ++i)
            {
                CurrentRule = OldRule; //Ensure ancestor rules are recorded on this branch.

                CurrentRule += LHSAttributes[i] + " " + Comparators[i] + " " + RHSValues[i];

                //If this node is a leaf/contains a classifier value.
                if (Classifications.ContainsKey(i))
                {
                    CurrentRule += " ] ) --> " + Classifications[i];
                    AllRules.AppendLine(CurrentRule);
                }
                else //If the rule isn't fully built.
                {
                    CurrentRule += " ] & [ ";

                    //Call the recursive version of this method that will fully evaluate each path of the next node.
                    Nodes[i].WriteBranch(CurrentRule, ref AllRules);
                }
            }
        }

        private string WriteThisBranch(TreeNode Sender, string Branch = "", int MatchedChild = -1)
        {
            if(MatchedChild > -1)
            {
                Branch += LHSAttributes[MatchedChild] + " " + Comparators[MatchedChild] + " " + RHSValues[MatchedChild];
                Branch += " ] ) --> " + Classifications[MatchedChild];
            }
            foreach(KeyValuePair<int, TreeNode> n in Nodes)
            {
                if(n.Value.Equals(Sender))
                {
                    Branch = LHSAttributes[n.Key] + " " + Comparators[n.Key] + " " + RHSValues[n.Key] + " ] & [ " + Branch;
                }
            }
            if (Parent != null)
            {
                return Parent.WriteThisBranch(this, Branch);
            }
            else
            {
                Branch = "If ( [ " + Branch;
                return Branch;
            }
        }

        /// <summary>
        /// Get the classification target of a record.
        /// </summary>
        /// <param name="Record"></param>
        /// <returns></returns>
        public Classification Classify(DataRecord Record)
        {
            //For every child of this node.
            for (int i = 0; i < Comparators.Count; ++i)
            {

                switch (Comparators[i])
                {
                    case (">"):
                        if (Decimal.Parse(Record.GetAttributeValue(LHSAttributes[i])) > Decimal.Parse(RHSValues[i]))
                        {
                            if (Classifications.ContainsKey(i))
                            {
                                return new Classification(new string[] { Classifications[i] },
                                    new string[] { this.WriteThisBranch(null, "", i) });
                            }
                            else
                            {
                                return Nodes[i].Classify(Record);
                            }
                        }
                        break;
                    case ("<"):
                        if (Decimal.Parse(Record.GetAttributeValue(LHSAttributes[i])) < Decimal.Parse(RHSValues[i]))
                        {
                            if (Classifications.ContainsKey(i))
                            {
                                return new Classification(new string[] { Classifications[i] },
                                    new string[] { this.WriteThisBranch(null, "", i) });
                            }
                            else
                            {
                                return Nodes[i].Classify(Record);
                            }
                        }
                        break;
                    case (">="):
                        if (Decimal.Parse(Record.GetAttributeValue(LHSAttributes[i])) >= Decimal.Parse(RHSValues[i]))
                        {
                            if (Classifications.ContainsKey(i))
                            {
                                return new Classification(new string[] { Classifications[i] },
                                    new string[] { this.WriteThisBranch(null, "", i) });
                            }
                            else
                            {
                                return Nodes[i].Classify(Record);
                            }
                        }
                        break;
                    case ("<="):
                        if (Decimal.Parse(Record.GetAttributeValue(LHSAttributes[i])) <= Decimal.Parse(RHSValues[i]))
                        {
                            if (Classifications.ContainsKey(i))
                            {
                                return new Classification(new string[] { Classifications[i] },
                                    new string[] { this.WriteThisBranch(null, "", i) });
                            }
                            else
                            {
                                return Nodes[i].Classify(Record);
                            }
                        }
                        break;
                    case ("="):
                        if (Record.GetAttributeValue(LHSAttributes[i]) == RHSValues[i])
                        {
                            if (Classifications.ContainsKey(i))
                            {
                                return new Classification(new string[] { Classifications[i] },
                                    new string[] { this.WriteThisBranch(null, "", i) });
                            }
                            else
                            {
                                return Nodes[i].Classify(Record);
                            }
                        }
                        break;
                    default:
                        throw new Exception("Invalid comparator: " + Comparators[i]);
                }
            }
            throw new Exception("Invalid data value or incomplete tree.");
        }

        public TreeNode(List<string> WekaOutput, TreeNode Parent)
        {
            Nodes = new Dictionary<int, TreeNode>();
            LHSAttributes = new List<string>();
            RHSValues = new List<string>();
            Comparators = new List<string>();
            Classifications = new Dictionary<int, string>();
            this.Parent = Parent;
            this.ParseTreeSection(WekaOutput);
        }
    }
}
