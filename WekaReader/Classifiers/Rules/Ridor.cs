using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WekaReader.Classifiers.Rules
{
    class Ridor : RuleModel
    {
        public Ridor(string WekaOutput)
    : base(WekaOutput)
        { }
        public Ridor() { }
        protected override List<string> ReadWekaOutput(string WekaOutput)
        {
            string[] SplitInput = WekaOutput.Replace("\t", String.Empty).Replace("\r", String.Empty).Split('\n');
            List<string> RulesText = new List<string>();
            int line = 0;

            string RuleStart = "RIpple DOwn Rule Learner(Ridor) rules";

            while (SplitInput[line].Trim() != RuleStart)
            {
                ++line;
            }
            line += 3;
            while (SplitInput[line] != "")
            {
                RulesText.Add(SplitInput[line].Trim());
                ++line;
            }
            return RulesText;
        }
        protected override List<Rule> ReadRules(List<string> WekaFormattedRules)
        {
            string classifierAttribute = WekaFormattedRules[0].Split(new string[] { " = " }, StringSplitOptions.None)[0];
            DefaultClassification = WekaFormattedRules[0].Split(new string[] { " = " }, StringSplitOptions.None)[1].Split(' ')[0];

            List<Rule> rules = new List<Rule>();

            for (int line = 1; line < WekaFormattedRules.Count; ++line)
            {
                Rule currentRule = new Rule();

                string[] splitSpaces = WekaFormattedRules[line].Split(' ');
                string trimmedLine = "";

                for (int i = 1; i < splitSpaces.Count() - 2; ++i)
                {
                    trimmedLine += splitSpaces[i] + " ";
                }

                currentRule.Classification = trimmedLine.Split(new string[] { " => " }, StringSplitOptions.None)[1].Split(' ')[2];

                string componentString = trimmedLine.Split(new string[] { " => " }, StringSplitOptions.None)[0];
                string[] components = componentString.Split(new string[] { " and " }, StringSplitOptions.None);

                foreach (string component in components)
                {
                    string[] splitComponent = component.Substring(1, component.Length - 2).Split(' ');
                    string LHSAttribute = splitComponent[0];
                    Rule.EComparator comparator = Rule.EComparator.Equal;
                    switch (splitComponent[1])
                    {
                        case (">"):
                            comparator = Rule.EComparator.GreaterThan;
                            break;
                        case ("<"):
                            comparator = Rule.EComparator.LessThan;
                            break;
                        case ("<="):
                            comparator = Rule.EComparator.LessThanInclusive;
                            break;
                        case (">="):
                            comparator = Rule.EComparator.GreaterThanInclusive;
                            break;
                        case ("="):
                            comparator = Rule.EComparator.Equal;
                            break;
                    }
                    currentRule.AddCondition(new Rule.RuleComponent(splitComponent[0], new List<Rule.EComparator>() { comparator }, new List<string>() { splitComponent[2] }));
                }
                rules.Add(currentRule);
            }
            return rules;
        }
        public override bool IsThisModelType(string Model)
        {
            if (Model.Split('\n').Length > 1)
            {
                return Model.Split('\n')[2].Split(' ')[0] == "Scheme:weka.classifiers.rules.Ridor";
            }
            else
            {
                return false;
            }
        }
    }
}
