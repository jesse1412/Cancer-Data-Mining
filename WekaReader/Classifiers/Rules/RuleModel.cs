using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using WekaReader.Data;

namespace WekaReader.Classifiers.Rules
{
    public abstract class RuleModel : AbstractClassifier
    {
        public string DefaultClassification { get; set; }

        public List<Rule> Rules
        {
            get
            {
                return AllRules;
            }
        }

        protected abstract List<Rule> ReadRules(List<string> WekaFormattedRules);

        /// <summary>
        /// Returns lines defining the rules from the weka output.
        /// </summary>
        /// <param name="Input">Weka output for tree</param>
        /// <returns></returns>
        protected abstract List<string> ReadWekaOutput(string WekaOutput);

        public RuleModel(string WekaOutput, string DefaultClassification = "Unclassified")
        {
            this.DefaultClassification = DefaultClassification;
            AllRules = ReadRules(ReadWekaOutput(WekaOutput));
        }

        protected RuleModel() { }

        public override string GetStringOfRules()
        {
            StringBuilder Output = new StringBuilder();
            Output.AppendLine("STANDARDISED RULE FORMAT");
            foreach (Rule r in AllRules)
            {
                Output.AppendLine(r.GetRule());
            }
            Output.AppendLine("DEFAULT: " + DefaultClassification);
            return Output.ToString();
        }

        public override Classification Classify(DataRecord Record)
        {
            Classification classification = new Classification();
            foreach (Rule r in Rules)
            {
                bool satisfied = true;
                foreach (Rule.RuleComponent rc in r.Components)
                {
                    if (satisfied)
                    {
                        bool localSatisfied = false;
                        for (int rcIndex = 0; rcIndex < rc.Comparators.Length; ++rcIndex)
                        {
                            if (!localSatisfied)
                            {
                                switch (rc.Comparators[rcIndex])
                                {
                                    case Rule.EComparator.Equal:
                                        decimal lhs, rhs;
                                        if(decimal.TryParse(Record.GetAttributeValue(rc.LHSAttribute), out lhs))
                                        {
                                            if (decimal.TryParse(rc.RHSValues[rcIndex], out rhs))
                                            {
                                                localSatisfied = lhs == rhs;
                                                break;
                                            }
                                        }
                                        localSatisfied = Record.GetAttributeValue(rc.LHSAttribute) == rc.RHSValues[rcIndex];
                                        break;
                                    case Rule.EComparator.GreaterThan:
                                        localSatisfied = Decimal.Parse(Record.GetAttributeValue(rc.LHSAttribute)) > Decimal.Parse(rc.RHSValues[rcIndex]);
                                        break;
                                    case Rule.EComparator.LessThan:
                                        localSatisfied = Decimal.Parse(Record.GetAttributeValue(rc.LHSAttribute)) < Decimal.Parse(rc.RHSValues[rcIndex]);
                                        break;
                                    case Rule.EComparator.GreaterThanInclusive:
                                        localSatisfied = Decimal.Parse(Record.GetAttributeValue(rc.LHSAttribute)) >= Decimal.Parse(rc.RHSValues[rcIndex]);
                                        break;
                                    case Rule.EComparator.LessThanInclusive:
                                        localSatisfied = Decimal.Parse(Record.GetAttributeValue(rc.LHSAttribute)) <= Decimal.Parse(rc.RHSValues[rcIndex]);
                                        break;
                                }
                            }
                            else
                            {
                                break;
                            }
                        }
                        satisfied = localSatisfied;
                    }
                    else
                    {
                        break;
                    }
                }
                if (satisfied)
                {
                    classification.AddMatch(r.Classification, r.GetRule());
                    satisfied = false;
                }
            }
            if(classification.Classifications.Length == 0)
            {
                classification.AddMatch(DefaultClassification, "None");
            }
            return classification;
        }
    }
}
