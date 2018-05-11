using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace WekaReader.Classifiers.Rules
{
    class NNGE : RuleModel
    {
        static Regex Operators = new Regex("<|>|<=|>=|=");
        public NNGE(string WekaOutput)
    : base(WekaOutput)
        { }
        public NNGE()
        {
        }
        protected override List<string> ReadWekaOutput(string WekaOutput)
        {
            string[] SplitInput = WekaOutput.Replace("\t", String.Empty).Replace("\r", String.Empty).Split('\n');
            List<string> RulesText = new List<string>();
            int line = 0;

            Regex RuleStart = new Regex(".*Rules generated.*");

            while (!RuleStart.Match(SplitInput[line].Trim()).Success)
            {
                ++line;
            }
            ++line;
            while (SplitInput[line] != "")
            {
                RulesText.Add(SplitInput[line].Trim());
                ++line;
            }
            return RulesText;
        }
        protected override List<Rule> ReadRules(List<string> WekaFormattedRules)
        {
            Rule currentRule = null;
            List<Rule> rules = new List<Rule>();

            string classification = String.Empty;
            string Attribute = String.Empty;
            string[] Values = null;

            List<string> RHSValuesList = new List<string>();
            List<Rule.EComparator> comparators = new List<Rule.EComparator>();

            string[] splitLineClassifier;
            string[] splitLineRuleComponents;
            string[] splitLHSRHS;
            string[] splitRHSValues;

            foreach (string line in WekaFormattedRules)
            {
                currentRule = new Rule();
                splitLineClassifier = line.Split(new string[] { "  (" }, StringSplitOptions.None)[0].Split(new string[] { " : " }, StringSplitOptions.None);
                splitLineRuleComponents = splitLineClassifier[1].Split(new string[] { " ^ " }, StringSplitOptions.None);

                classification = splitLineClassifier[0];
                classification = classification.Substring(6, classification.Length - 9);

                currentRule.Classification = classification;

                foreach (string ruleComponent in splitLineRuleComponents)
                {
                    comparators = new List<Rule.EComparator>();
                    RHSValuesList = new List<string>();

                    if ((splitLHSRHS = ruleComponent.Split(new string[] { " in " }, StringSplitOptions.None)).Length > 1)
                    {
                        Attribute = splitLHSRHS[0];
                        splitLHSRHS[1] = splitLHSRHS[1].Substring(1, splitLHSRHS[1].Length - 2);
                        splitRHSValues = splitLHSRHS[1].Split(',');

                        Values = splitRHSValues;

                        for (int i = 0; i < Values.Length; ++i)
                        {
                            comparators.Add(Rule.EComparator.Equal);
                        }
                    }
                    else
                    {
                        int Operand1EndIndex = 0;
                        int Operand3StartIndex = 0;
                        int Operand2StartIndex = 0;
                        int Operand2EndIndex = ruleComponent.Length;
                        int operandCount = 1;

                        for (int i = 0; i < ruleComponent.Length - 1; ++i)
                        {
                            if (Operators.Match(ruleComponent[i].ToString()).Success)
                            {
                                operandCount++;
                                if (Operand1EndIndex == 0)
                                {
                                    if (Operators.Match(ruleComponent[i + 1].ToString()).Success)
                                    {
                                        Operand1EndIndex = i - 1;
                                        Operand2StartIndex = i + 2;
                                        ++i;
                                    }
                                    else
                                    {
                                        Operand1EndIndex = i - 1;
                                        Operand2StartIndex = i + 1;
                                    }
                                }
                                else
                                {
                                    Operand2EndIndex = i - 1;
                                    if (Operators.Match(ruleComponent[i + 1].ToString()).Success)
                                    {
                                        Operand3StartIndex = i + 2;
                                        ++i;
                                    }
                                    else
                                    {
                                        Operand3StartIndex = i + 1;
                                    }
                                }
                                ++i;
                            }
                        }

                        if (operandCount < 3)
                        {
                            Attribute = ruleComponent.Substring(0, Operand1EndIndex + 1);
                            switch (ruleComponent.Substring(Operand1EndIndex + 1, Operand2StartIndex - 1 - (Operand1EndIndex)))
                            {
                                case ">":
                                    comparators.Add(Rule.EComparator.GreaterThan);
                                    break;
                                case "=":
                                    comparators.Add(Rule.EComparator.Equal);
                                    break;
                                case "<":
                                    comparators.Add(Rule.EComparator.LessThan);
                                    break;
                                case ">=":
                                    comparators.Add(Rule.EComparator.GreaterThanInclusive);
                                    break;
                                case "<=":
                                    comparators.Add(Rule.EComparator.LessThanInclusive);
                                    break;
                            }
                            RHSValuesList.Add(ruleComponent.Substring(Operand2StartIndex, ruleComponent.Length - Operand2StartIndex));
                        }
                        else
                        {
                            Attribute = ruleComponent.Substring(Operand2StartIndex, Operand2EndIndex - Operand2StartIndex + 1);
                            string RHSValue1 = ruleComponent.Substring(0, Operand1EndIndex + 1);
                            switch (ruleComponent.Substring(Operand1EndIndex + 1, Operand2StartIndex - 1 - (Operand1EndIndex)))
                            {
                                case ">":
                                    //comparators.Add(Rule.EComparator.LessThan);
                                    currentRule.AddCondition(new Rule.RuleComponent(Attribute, new List<Rule.EComparator>() { Rule.EComparator.LessThan }, new string[] { RHSValue1 }));
                                    break;
                                case "=":
                                    currentRule.AddCondition(new Rule.RuleComponent(Attribute, new List<Rule.EComparator>() { Rule.EComparator.Equal }, new string[] { RHSValue1 }));
                                    break;
                                case "<":
                                    currentRule.AddCondition(new Rule.RuleComponent(Attribute, new List<Rule.EComparator>() { Rule.EComparator.GreaterThan }, new string[] { RHSValue1 }));
                                    break;
                                case ">=":
                                    currentRule.AddCondition(new Rule.RuleComponent(Attribute, new List<Rule.EComparator>() { Rule.EComparator.LessThanInclusive }, new string[] { RHSValue1 }));
                                    break;
                                case "<=":
                                    currentRule.AddCondition(new Rule.RuleComponent(Attribute, new List<Rule.EComparator>() { Rule.EComparator.GreaterThanInclusive }, new string[] { RHSValue1 }));
                                    break;
                            }
                            switch (ruleComponent.Substring(Operand2EndIndex + 1, Operand3StartIndex - 1 - (Operand2EndIndex)))
                            {
                                case ">":
                                    comparators.Add(Rule.EComparator.GreaterThan);
                                    break;
                                case "=":
                                    comparators.Add(Rule.EComparator.Equal);
                                    break;
                                case "<":
                                    comparators.Add(Rule.EComparator.LessThan);
                                    break;
                                case ">=":
                                    comparators.Add(Rule.EComparator.GreaterThanInclusive);
                                    break;
                                case "<=":
                                    comparators.Add(Rule.EComparator.LessThanInclusive);
                                    break;
                            }
                            RHSValuesList.Add(ruleComponent.Substring(Operand3StartIndex, ruleComponent.Length - Operand3StartIndex));
                        }
                        Values = RHSValuesList.ToArray();
                    }
                    currentRule.AddCondition(new Rule.RuleComponent(Attribute, comparators, Values));
                }
                rules.Add(currentRule);
            }
            return rules;
        }
        public override bool IsThisModelType(string Model)
        {
            if (Model.Split('\n').Length > 1)
            {
                return Model.Split('\n')[2].Split(' ')[0] == "Scheme:weka.classifiers.rules.NNge";
            }
            else
            {
                return false;
            }
        }
    }
}
