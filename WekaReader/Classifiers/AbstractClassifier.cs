using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WekaReader.Classifiers;
using WekaReader.Data;

namespace WekaReader.Classifiers
{
    public abstract class AbstractClassifier
    {
        private List<Rule> allRules;
        public List<Rule> AllRules
        {
            get
            {
                return allRules;
            }
            protected set
            {
                this.allRules = value;
            }
        }
        public string Name { get; set; }

        public abstract string GetStringOfRules();

        public abstract Classification Classify(DataRecord Record);

        public abstract bool IsThisModelType(string Model);
    }
    public class Rule
    {
        public class RuleComponent
        {
            public string LHSAttribute;
            public EComparator[] Comparators;
            public string[] RHSValues;

            public RuleComponent(string LHSAttribute, List<EComparator> Comparators, List<string> RHSValues)
            {
                this.LHSAttribute = LHSAttribute;
                this.Comparators = Comparators.ToArray();
                this.RHSValues = RHSValues.ToArray();
            }
            public RuleComponent(string LHSAttribute, EComparator[] Comparators, List<string> RHSValues)
            {
                this.LHSAttribute = LHSAttribute;
                this.Comparators = Comparators;
                this.RHSValues = RHSValues.ToArray();
            }
            public RuleComponent(string LHSAttribute, EComparator[] Comparators, string[] RHSValues)
            {
                this.LHSAttribute = LHSAttribute;
                this.Comparators = Comparators;
                this.RHSValues = RHSValues;
            }
            public RuleComponent(string LHSAttribute, List<EComparator> Comparators, string[] RHSValues)
            {
                this.LHSAttribute = LHSAttribute;
                this.Comparators = Comparators.ToArray();
                this.RHSValues = RHSValues;
            }
        }
        public enum EComparator
        {
            GreaterThan,
            LessThan,
            GreaterThanInclusive,
            LessThanInclusive,
            Equal
        }
        private List<RuleComponent> components;
        public List<RuleComponent> Components
        {
            get { return components; }
            protected set { this.components = value; }
        }
        public string Classification { get; set; }
        public Rule()
        {
            Components = new List<RuleComponent>();
            Classification = "";
        }
        public void AddCondition(RuleComponent component)
        {
            Components.Add(component);
        }
        public string GetRule()
        {
            string returnString = "";
            returnString += "If ( ";
            foreach (RuleComponent component in Components)
            {
                returnString += "[ ";
                for (int i = 0; i < component.RHSValues.Count(); ++i)
                {
                    returnString += component.LHSAttribute;
                    switch (component.Comparators[i])
                    {
                        case (EComparator.Equal):
                            returnString += " = ";
                            break;
                        case (EComparator.GreaterThan):
                            returnString += " > ";
                            break;
                        case (EComparator.GreaterThanInclusive):
                            returnString += " >= ";
                            break;
                        case (EComparator.LessThan):
                            returnString += " < ";
                            break;
                        case (EComparator.LessThanInclusive):
                            returnString += " <= ";
                            break;
                    }
                    returnString += component.RHSValues[i];

                    if (i < component.RHSValues.Count() - 1)
                    {
                        returnString += " OR ";
                    }
                    else if (i < component.RHSValues.Count())
                    {
                        returnString += " ]";
                        if (component != Components.Last())
                        {
                            returnString += " & ";
                        }
                    }
                }
            }
            returnString += " ) --> " + Classification;
            return returnString;
        }
    }
}
