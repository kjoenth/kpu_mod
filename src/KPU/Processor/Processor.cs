﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KPU.Processor
{
    public class Instruction
    {
        public class ParseError : System.Exception
        {
            public string mErrText;
            public ParseError(string errText)
            {
                mErrText = errText;
            }
            public string ToString()
            {
                return mErrText;
            }
        }
        public string mText;

        private bool lastValue = false;
        private bool edgeTriggered;

        /*
Tokens:
    keywords: ON DO IF THEN AND OR
    operators: < <= == >= > != + - * / . !
    literals: [0-9]+
    identifiers: [a-zA-Z_]+
Syntax:
    program ::= stmt ; program?
    stmt    ::= on-stmt | if-stmt | act-list
    on-stmt ::= ON expr DO action-list
    if-stmt ::= IF expr THEN action-list
    act-list::= action (, action-list)?
    expr    ::= un-op expr | bin-op expr expr | dot-expr | literal
    un-op   ::= !
    bin-op  ::= log-op | comp-op | arith-op
    log-op  ::= AND | OR
    comp-op ::= < | <= | == | >= | > | !=
    arith-op::= + | - | * | /
    dot-expr::= (dot-expr .)? identifier # we're keeping infix dot-expr, that's not too hard to do
    action  ::= dot-expr expr-lst?
    expr-lst::= expr (, expr-lst)?
        */
        private enum Tokens { TOK_KEYWORD, TOK_LOG_OP, TOK_COMP_OP, TOK_ARITH_OP, TOK_UN_OP, TOK_DOT, TOK_LITERAL, TOK_IDENT, TOK_WHITESPACE };
        private List<KeyValuePair<string, Tokens>> Tokenise(string text)
        {
            Logging.Log("Attempting to tokenise " + text);
            List<KeyValuePair<string, Tokens>> result = new List<KeyValuePair<string, Tokens>>();
            Dictionary<string, Tokens> TokenDict = new Dictionary<string, Tokens>() {
                {"ON", Tokens.TOK_KEYWORD},
                {"DO", Tokens.TOK_KEYWORD},
                {"IF", Tokens.TOK_KEYWORD},
                {"THEN", Tokens.TOK_KEYWORD},
                {"AND", Tokens.TOK_LOG_OP},
                {"OR", Tokens.TOK_LOG_OP},
                {"[<=>]=?", Tokens.TOK_COMP_OP},
                {"!=", Tokens.TOK_COMP_OP},
                {"[+\\-*/]", Tokens.TOK_ARITH_OP},
                {"!", Tokens.TOK_UN_OP},
                {"\\.", Tokens.TOK_DOT},
                {"[0-9]+", Tokens.TOK_LITERAL},
                {"[a-z][a-zA-Z_]*", Tokens.TOK_IDENT},
                {"\\s", Tokens.TOK_WHITESPACE},
            };
            int index = 0;
            while (index < text.Length)
            {
                Dictionary<Tokens, string> Matches = new Dictionary<Tokens, string>();
                foreach (KeyValuePair<string, Tokens> kvp in TokenDict)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(text.Substring(index), "^" + kvp.Key);
                    if (match.Success)
                    {
                        Matches.Add(kvp.Value, match.Value);
                    }
                }
                if (Matches.Count == 0)
                {
                    throw new ParseError(string.Format("No matches for {0}", text.Substring(index)));
                }
                int max_len = Matches.Max(kvp => kvp.Value.Length);
                IEnumerable<KeyValuePair<Tokens, string>> MaxMatches = Matches.Where(kvp => kvp.Value.Length == max_len);
                if (MaxMatches.Count() != 1)
                {
                    foreach (KeyValuePair<Tokens, string> kvp in MaxMatches)
                        Logging.Log(kvp.Key.ToString() + ": " + kvp.Value);
                    throw new ParseError(string.Format("{0:D} matches for {1}", MaxMatches.Count(), text.Substring(index)));
                }
                string matchedText = MaxMatches.First().Value;
                Tokens matchedToken = MaxMatches.First().Key;
                if (matchedToken != Tokens.TOK_WHITESPACE)
                    result.Add(new KeyValuePair<string, Tokens>(matchedText, matchedToken));
                if (matchedText.Length < 1)
                {
                    throw new ParseError(string.Format("Match {0} is too short", matchedText));
                }
                index += matchedText.Length;
                Logging.Log(string.Format("Accepted token {0} as {1:G}", matchedText, matchedToken));
            }
            Logging.Log("Tokenisation complete!");
            return result;
        }

        public Instruction (string text)
        {
            mText = text;
            KPU.Logging.Log("Instruction: Parser not written yet!");
            List<KeyValuePair<string, Tokens>> tokens = Tokenise(text);
        }

        public int imemWords { get { return 0; } }

        private bool eval(Processor p)
        {
            return false;
        }

        public bool checkTrigger(Processor p)
        {
            if (edgeTriggered)
            {
                bool oldValue = lastValue, newValue = eval(p);
                lastValue = newValue;
                return newValue && !oldValue;
            }
            else
            {
                return eval(p);
            }
        }

        public void exec(Processor p)
        {
        }
    }

    public enum InputType { BOOLEAN, DOUBLE };

    public class InputValue
    {
        public InputType typ;
        public bool Bool;
        public double Double;

        public InputValue(double value)
        {
            typ = InputType.DOUBLE;
            Double = value;
        }

        public InputValue(bool value)
        {
            typ = InputType.BOOLEAN;
            Bool = value;
        }

        public override string ToString()
        {
            switch(typ)
            {
            case InputType.BOOLEAN:
                return Bool ? "1" : "0";
            case InputType.DOUBLE:
                return Double.ToString("g4");
            default: // can't happen
                return typ.ToString();
            }
        }
    }

    public interface IInputData
    {
        string name { get; }
        bool available { get; }
        InputType typ { get; }
        InputValue value { get; }
    }

    public class Batteries : IInputData
    {
        public string name { get { return "batteries"; } }
        public bool available { get { return TotalElectricChargeCapacity > 0.1f; }}
        public InputType typ {get { return InputType.DOUBLE; } }
        public InputValue value { get { return new InputValue(ElectricChargeFillLevel * 100.0f); } }
        private Vessel parentVessel = null;

        public Batteries (Vessel v)
        {
            parentVessel = v;
        }

        private IEnumerable<PartResource> ElectricChargeResources
        {
            get
            {
                if (this.parentVessel != null && this.parentVessel.rootPart != null) {
                    int ecid = PartResourceLibrary.Instance.GetDefinition ("ElectricCharge").id;
                    List<PartResource> resources = new List<PartResource> ();
                    this.parentVessel.rootPart.GetConnectedResources (ecid, ResourceFlowMode.ALL_VESSEL, resources);
                    return resources;
                }
                return new List<PartResource>();
            }
        }

        private double TotalElectricCharge
        {
            get { return this.ElectricChargeResources.Sum(x => x.amount); }
        }

        private double TotalElectricChargeCapacity
        {
            get { return this.ElectricChargeResources.Sum(x => x.maxAmount); }
        }

        private double ElectricChargeFillLevel
        {
            get { return this.TotalElectricChargeCapacity > 0.1f ? this.TotalElectricCharge / this.TotalElectricChargeCapacity : 0f; }
        }

    }

    public class SensorDriven
    {
        public virtual string name { get { return "abstract"; } }
        public Vessel parentVessel;
        public bool available
        {
            get
            {
                return parentVessel.Parts.Any(p => p.FindModulesImplementing<KPU.Modules.ModuleKpuSensor>().Any(m => m.sensorType.Equals(name)));
            }
        }
        public double res { get {
            double rv = Double.PositiveInfinity;
            foreach (Part p in parentVessel.Parts)
                foreach(KPU.Modules.ModuleKpuSensor m in p.FindModulesImplementing<KPU.Modules.ModuleKpuSensor>())
                    if (m.sensorType.Equals(name))
                        if (m.sensorRes < rv)
                            rv = m.sensorRes;
            return rv;
            }}
    }

    public class Gear : SensorDriven, IInputData
    {
        public override string name { get { return "gear"; } }
        public InputType typ { get { return InputType.BOOLEAN; } }
        public InputValue value
        {
            get
            {
                Vessel.Situations situation = parentVessel.situation;
                return new InputValue(situation == Vessel.Situations.LANDED || situation == Vessel.Situations.PRELAUNCH);
            }
        }

        public Gear (Vessel v)
        {
            parentVessel = v;
        }
    }

    public class SrfHeight : SensorDriven, IInputData
    {
        public override string name { get { return "srfHeight"; } }
        public InputType typ { get { return InputType.DOUBLE; } }
        public InputValue value
        {
            get
            {
                if (!available) return new InputValue(Double.PositiveInfinity);
                double h = parentVessel.altitude - parentVessel.terrainAltitude;
                return new InputValue(Math.Round(h / res) * res);
            }
        }

        public SrfHeight (Vessel v)
        {
            parentVessel = v;
        }
    }

    public class SrfSpeed : SensorDriven, IInputData
    {
        public override string name { get { return "srfSpeed"; } }
        public InputType typ { get { return InputType.DOUBLE; } }
        public InputValue value
        {
            get
            {
                if (!available) return new InputValue(Double.PositiveInfinity);
                double s = parentVessel.GetSrfVelocity().magnitude;
                return new InputValue(Math.Round(s / res) * res);
            }
        }

        public SrfSpeed (Vessel v)
        {
            parentVessel = v;
        }
    }

    public class Processor
    {
        public bool hasLevelTrigger, hasLogicOps, hasArithOps;
        int imemWords;
        public List<Instruction> instructions;

        private Part mPart;
        public bool isRunning = true;

        // Warning, may be null
        public Vessel parentVessel { get { return mPart.vessel; } }

        //private ProcessorWindow mWindow;

        private List<IInputData> inputs = new List<IInputData>();

        public Processor (Part part, Modules.ModuleKpuProcessor module)
        {
            mPart = part;
            hasLevelTrigger = module.hasLevelTrigger;
            hasLogicOps = module.hasLogicOps;
            hasArithOps = module.hasArithOps;
            imemWords = module.imemWords;
            instructions = new List<Instruction>();
            inputs.Add(new Batteries(parentVessel));
            inputs.Add(new Gear(parentVessel));
            inputs.Add(new SrfHeight(parentVessel));
            inputs.Add(new SrfSpeed(parentVessel));

            // Short program (autolander) for testing
            //AddInstruction("ON < altitude 10000 DO orient.hold srfRetrograde, engine.activate");
            AddInstruction("ON < srfSpeed / srfHeight 100 DO throttle.set 0");
            AddInstruction("ON < srfHeight 250 DO gear.extend");
            AddInstruction("ON AND gear.landingSensor < srfHeight 50 DO engine.deactivate, orient.hold srfCustom 90 90 90");
            AddInstruction("IF > srfSpeed / srfHeight 16 THEN throttle.incr 25");
            AddInstruction("IF < srfSpeed / srfHeight 20 THEN throttle.decr 25");
        }

        public bool AddInstruction(string text)
        {
            Instruction i;
            try
            {
                i = new Instruction(text);
            }
            catch (Instruction.ParseError exc)
            {
                Logging.Log(exc.ToString());
                return false;
            }
            if (i.imemWords > imemWords)
                return false;
            instructions.Add(i);
            imemWords -= i.imemWords;
            Logging.Log("Added instruction: " + i.mText);
            return true;
        }

        public Dictionary<string, InputValue> inputValues;

        public void OnUpdate ()
        {
            inputValues = new Dictionary<string, InputValue>();
            foreach (IInputData i in inputs)
            {
                if (i.available)
                {
                    inputValues.Add(i.name, i.value);
                }
            }
            if (isRunning)
            {
                foreach (Instruction i in instructions)
                {
                    if (i.checkTrigger(this))
                    {
                        i.exec(this);
                    }
                }
            }
        }

        public void Save (ConfigNode node)
        {
            if (node.HasNode("Processor"))
                node.RemoveNode("Processor");
            
            ConfigNode Proc = new ConfigNode("Processor");

            // TODO store instructions list in Proc

            node.AddNode(Proc);
        }

        public void Load (ConfigNode node)
        {
            if (!node.HasNode("Processor"))
                return;
            
            ConfigNode Proc = node.GetNode("Processor");

            // TODO read instructions list from Proc
        }
    }
}

