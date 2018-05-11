using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WekaReader.Classifiers.Rules
{
    class Standardised : RuleModel
    {
        public Standardised(string WekaOutput)
            : base(WekaOutput)
        { }
        public Standardised() { }
        protected override List<string> ReadWekaOutput(string WekaOutput)
        {
            string[] SplitInput = WekaOutput.Replace("\t", String.Empty).Replace("\r", String.Empty).Split('\n');
            List<string> RulesText = new List<string>();

            foreach(string line in SplitInput)
            {
                if(line.Trim() == "")
                {
                    RulesText.RemoveAt(0);
                    return RulesText;
                }
                RulesText.Add(line);
            }
            return RulesText;
        }
        protected override List<Rule> ReadRules(List<string> WekaFormattedRules)
        {
            List<Rule> rules = new List<Rule>();
            foreach (string line in WekaFormattedRules)
            {
                if(line.Split(' ')[0] == "DEFAULT:")
                {
                    DefaultClassification = line.Split(' ')[1];
                    return rules;
                }
                string LHSAttribute = "";
                List<string> RHSValues = new List<string>();
                List<Rule.EComparator> comparators = new List<Rule.EComparator>();

                Rule currentRule = new Rule() { Classification = line.Split(new string[] { " --> " }, StringSplitOptions.None)[1] };
                string trimmedLine = line.Split(new string[] { " ] ) --> " }, StringSplitOptions.None)[0].Substring(7, line.Split(new string[] { " ] ) --> " }, StringSplitOptions.None)[0].Length - 7);
                string[] components = trimmedLine.Split(new string[] { " ] & [ " }, StringSplitOptions.None);

                foreach(string component in components)
                {
                    RHSValues = new List<string>();
                    comparators = new List<Rule.EComparator>();

                    string[] subComponents = component.Split(new string[] { " OR " }, StringSplitOptions.None);
                    foreach(string subComponent in subComponents)
                    {
                        string[] splitstring = subComponent.Split(' ');
                        LHSAttribute = splitstring[0];
                        switch (splitstring[1]) 
                        {
                            case (">"):
                                comparators.Add(Rule.EComparator.GreaterThan);
                                break;
                            case ("<"):
                                comparators.Add(Rule.EComparator.LessThan);
                                break;
                            case ("<="):
                                comparators.Add(Rule.EComparator.LessThanInclusive);
                                break;
                            case (">="):
                                comparators.Add(Rule.EComparator.GreaterThanInclusive);
                                break;
                            case ("="):
                                comparators.Add(Rule.EComparator.Equal);
                                break;
                        }
                        RHSValues.Add(splitstring[2]);
                    }
                    currentRule.AddCondition(new Rule.RuleComponent(LHSAttribute, comparators, RHSValues));
                }
                rules.Add(currentRule);
            }
            return rules;
        }
        public override bool IsThisModelType(string Model)
        {
            return Model.Split('\r')[0] == "STANDARDISED RULE FORMAT";
        }
    }
}
