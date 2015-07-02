﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KPU.Processor
{
    public class Instruction
    {
        public class ErrorMessage : System.Exception
        {
            public string mErrText;
            public ErrorMessage(string errText)
            {
                mErrText = errText;
            }
            public override string ToString()
            {
                return mErrText;
            }
        }
        public class ParseError : ErrorMessage
        {
            public ParseError(string errText)
               : base(errText)
            {
}
        }
        public class EvalError : ErrorMessage
        {
            public EvalError(string errText)
               : base(errText)
            {
            }
        }
        public class ExecError : ErrorMessage
        {
            public ExecError(string errText)
               : base(errText)
            {
            }
        }
        public string mText;

        public bool skip = false;
        public bool lastValue = false;

        public enum Tokens { TOK_COMMENT, TOK_KEYWORD, TOK_LOG_OP, TOK_COMP_OP, TOK_ARITH_OP, TOK_UN_OP, TOK_AT, TOK_COMMA, TOK_SEMI, TOK_LITERAL, TOK_IDENT, TOK_WHITESPACE };
        private List<KeyValuePair<string, Tokens>> Tokenise(string text)
        {
            //Logging.Log("Attempting to tokenise " + text);
            List<KeyValuePair<string, Tokens>> result = new List<KeyValuePair<string, Tokens>>();
            Dictionary<string, Tokens> TokenDict = new Dictionary<string, Tokens>() {
                {"#.*", Tokens.TOK_COMMENT},
                {"ON", Tokens.TOK_KEYWORD},
                {"DO", Tokens.TOK_KEYWORD},
                {"IF", Tokens.TOK_KEYWORD},
                {"THEN", Tokens.TOK_KEYWORD},
                {"AND", Tokens.TOK_LOG_OP},
                {"OR", Tokens.TOK_LOG_OP},
                {"[<>]", Tokens.TOK_COMP_OP},
                {"[+\\-*/]", Tokens.TOK_ARITH_OP},
                {"!", Tokens.TOK_UN_OP},
                {"@", Tokens.TOK_AT},
                {",", Tokens.TOK_COMMA},
                {";", Tokens.TOK_SEMI},
                {"-?[0-9]+(\\.[0-9]+)?", Tokens.TOK_LITERAL},
                {"[a-z][a-zA-Z_.]*", Tokens.TOK_IDENT},
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
                {
                    result.Add(new KeyValuePair<string, Tokens>(matchedText, matchedToken));
                    //Logging.Log(string.Format("Accepted token {0} as {1:G}", matchedText, matchedToken));
                }
                if (matchedText.Length < 1)
                {
                    throw new ParseError(string.Format("Match {0} is too short", matchedText));
                }
                index += matchedText.Length;
            }
            //Logging.Log("Tokenisation complete!");
            return result;
        }

        public class ASTNode
        {
            public KeyValuePair<string, Tokens> mToken;
            public List<ASTNode> mChildren;
            public ASTNode(KeyValuePair<string, Tokens> token)
            {
                mToken = token;
                mChildren = new List<ASTNode>();
            }
            public void Add(ASTNode child)
            {
                mChildren.Add(child);
            }
            public override string ToString()
            {
                string s = "";
                foreach (ASTNode n in mChildren)
                    s += " " + n.ToString();
                return string.Format("({0}:{1}{2})", mToken.Value.ToString(), mToken.Key, s);
            }
            public List<ASTNode> flat
            {
                get
                {
                    List<ASTNode> ret = new List<ASTNode>();
                    ret.Add(this);
                    foreach (ASTNode n in mChildren)
                        ret.AddRange(n.flat);
                    return ret;
                }
            }
        }

        public ASTNode mAST;
        private ASTNode LexRecursive(IEnumerator<KeyValuePair<string, Tokens>> tokens)
        {
            if (!tokens.MoveNext())
                throw new ParseError("Out of tokens");
            KeyValuePair<string, Tokens> token = tokens.Current;
            ASTNode n = new ASTNode(token);
            ASTNode left, right, child, cond, actn;
            switch(token.Value)
            {
            case Tokens.TOK_COMMENT:
                break;
            case Tokens.TOK_KEYWORD:
                if (token.Key.Equals("ON"))
                {
                    cond = LexRecursive(tokens);
                    if (cond.mToken.Value == Tokens.TOK_KEYWORD)
                        throw new ParseError("ON cond was bad: " + cond.ToString());
                    n.Add(cond);
                    actn = LexRecursive(tokens);
                    if (actn.mToken.Value != Tokens.TOK_KEYWORD || !actn.mToken.Key.Equals("DO"))
                        throw new ParseError("ON DO was bad: " + actn.ToString());
                    n.Add(actn);
                    break;
                }
                if (token.Key.Equals("IF"))
                {
                    cond = LexRecursive(tokens);
                    if (cond.mToken.Value == Tokens.TOK_KEYWORD)
                        throw new ParseError("IF cond was bad: " + cond.ToString());
                    n.Add(cond);
                    actn = LexRecursive(tokens);
                    if (actn.mToken.Value != Tokens.TOK_KEYWORD || !actn.mToken.Key.Equals("THEN"))
                        throw new ParseError("IF THEN was bad: " + actn.ToString());
                    n.Add(actn);
                    break;
                }
                if (token.Key.Equals("DO"))
                {
                    actn = LexRecursive(tokens);
                    if (actn.mToken.Value != Tokens.TOK_SEMI &&
                        actn.mToken.Value != Tokens.TOK_AT)
                        throw new ParseError("DO actn was bad: " + actn.ToString());
                    n.Add(actn);
                    break;
                }
                if (token.Key.Equals("THEN"))
                {
                    actn = LexRecursive(tokens);
                    if (actn.mToken.Value != Tokens.TOK_SEMI &&
                        actn.mToken.Value != Tokens.TOK_AT)
                        throw new ParseError("THEN actn was bad: " + actn.ToString());
                    n.Add(actn);
                    break;
                }
                throw new ParseError(token.Key); // can't happen
            case Tokens.TOK_LOG_OP:
            case Tokens.TOK_ARITH_OP:
            case Tokens.TOK_COMP_OP:
                left = LexRecursive(tokens);
                if (left.mToken.Value == Tokens.TOK_KEYWORD ||
                    left.mToken.Value == Tokens.TOK_AT ||
                    left.mToken.Value == Tokens.TOK_COMMA ||
                    left.mToken.Value == Tokens.TOK_SEMI)
                    throw new ParseError(token.Key + " left expr was bad: " + left.ToString());
                n.Add(left);
                right = LexRecursive(tokens);
                if (right.mToken.Value == Tokens.TOK_KEYWORD ||
                    right.mToken.Value == Tokens.TOK_AT ||
                    right.mToken.Value == Tokens.TOK_COMMA ||
                    right.mToken.Value == Tokens.TOK_SEMI)
                    throw new ParseError(token.Key + " right expr was bad: " + right.ToString());
                n.Add(right);
                break;
            case Tokens.TOK_UN_OP:
                child = LexRecursive(tokens);
                if (child.mToken.Value == Tokens.TOK_KEYWORD ||
                    child.mToken.Value == Tokens.TOK_AT ||
                    child.mToken.Value == Tokens.TOK_COMMA ||
                    child.mToken.Value == Tokens.TOK_SEMI)
                    throw new ParseError(token.Key + " child expr was bad: " + child.ToString());
                n.Add(child);
                break;
            case Tokens.TOK_IDENT:
            case Tokens.TOK_LITERAL:
                break;
            case Tokens.TOK_AT:
                left = LexRecursive(tokens);
                if (left.mToken.Value != Tokens.TOK_IDENT)
                    throw new ParseError(token.Key + " left expr was bad: " + left.ToString());
                n.Add(left);
                right = LexRecursive(tokens);
                if (right.mToken.Value == Tokens.TOK_KEYWORD ||
                    right.mToken.Value == Tokens.TOK_AT ||
                    right.mToken.Value == Tokens.TOK_SEMI)
                    throw new ParseError(token.Key + " right expr was bad: " + right.ToString());
                n.Add(right);
                break;
            case Tokens.TOK_COMMA:
                left = LexRecursive(tokens);
                if (left.mToken.Value == Tokens.TOK_KEYWORD ||
                    left.mToken.Value == Tokens.TOK_AT ||
                    left.mToken.Value == Tokens.TOK_SEMI)
                    throw new ParseError(token.Key + " left expr was bad: " + left.ToString());
                n.Add(left);
                right = LexRecursive(tokens);
                if (right.mToken.Value == Tokens.TOK_KEYWORD ||
                    right.mToken.Value == Tokens.TOK_AT ||
                    right.mToken.Value == Tokens.TOK_SEMI ||
                    right.mToken.Value == Tokens.TOK_COMMA)
                    throw new ParseError(token.Key + " right expr was bad: " + right.ToString());
                n.Add(right);
                break;
            case Tokens.TOK_SEMI:
                left = LexRecursive(tokens);
                if (left.mToken.Value != Tokens.TOK_IDENT &&
                    left.mToken.Value != Tokens.TOK_AT &&
                    left.mToken.Value != Tokens.TOK_SEMI)
                    throw new ParseError("; left expr was bad: " + left.ToString());
                n.Add(left);
                right = LexRecursive(tokens);
                if (right.mToken.Value != Tokens.TOK_IDENT &&
                    right.mToken.Value != Tokens.TOK_AT)
                    throw new ParseError("; right expr was bad: " + right.ToString());
                n.Add(right);
                break;
            default:
                throw new ParseError(token.Value.ToString() + ": " + token.Key); // can't happen
            }
            return n;
        }

        private ASTNode Lex(List<KeyValuePair<string, Tokens>> tokens)
        {
            IEnumerator<KeyValuePair<string, Tokens>> iTokens = tokens.GetEnumerator();
            ASTNode n = LexRecursive(iTokens);
            if (iTokens.MoveNext())
                throw new ParseError("Leftover token " + iTokens.Current.Value.ToString() + ": " + iTokens.Current.Key);
            if ((n.mToken.Value != Tokens.TOK_KEYWORD ||
                 (n.mToken.Key != "ON" && n.mToken.Key != "IF")) &&
                n.mToken.Value != Tokens.TOK_COMMENT)
                throw new ParseError("Bad root token " + iTokens.Current.Value.ToString() + ": " + iTokens.Current.Key);
            return n;
        }

        public override string ToString ()
        {
            StringBuilder sb = new StringBuilder();
            foreach (ASTNode n in mAST.flat)
            {
                sb.Append(n.mToken.Key);
                if (n.mToken.Value != Tokens.TOK_UN_OP &&
                    n.mToken.Value != Tokens.TOK_AT)
                {
                    sb.Append(" ");
                }
            }
            return sb.ToString().TrimEnd(' ');
        }

        public Instruction (string text)
        {
            mText = text;
            List<KeyValuePair<string, Tokens>> tokens = Tokenise(text);
            if (tokens[0].Value == Tokens.TOK_COMMENT)
                mImemWords = 0;
            else
                mImemWords = tokens.Count;
            //Logging.Log(string.Format("imemWords = {0:D}", mImemWords));
            mAST = Lex(tokens);
            //Logging.Log(mAST.ToString());
            mText = ToString();
        }

        private int mImemWords = 0;
        public int imemWords { get { return mImemWords; } }

        public enum Type { BOOLEAN, DOUBLE, NAME, TUPLE, VOID };

        public class Value
        {
            public Type typ;
            private bool mBool = false;
            private double mDouble = 0f;
            private string mName = null;
            private List<Value> mTuple = null;
            public bool b { get { return mBool; }}
            public double d { get { return mDouble; }}
            public string n { get { return mName; }}
            public List<Value> t { get { return mTuple; }}
            public Value(bool b) { typ = Type.BOOLEAN; mBool = b; }
            public Value(double d) { typ = Type.DOUBLE; mDouble = d; }
            public Value(string n) { typ = Type.NAME; mName = n; }
            public Value(Value car, Value cdr) { typ = Type.TUPLE; mTuple = new List<Value>(2){car, cdr}; }
            public Value() { typ = Type.VOID; }
            public List<Value> l { get {
                if (typ == Type.TUPLE)
                    return new List<Value>(t[0].l.Concat(t[1].l));
                return new List<Value>() {this};
            }}
            public override string ToString()
            {
                switch(typ)
                {
                case Type.BOOLEAN:
                    return mBool ? "True" : "False";
                case Type.DOUBLE:
                    return mDouble.ToString("g4");
                case Type.NAME:
                    return "[" + mName + "]";
                case Type.TUPLE:
                    string s = "(";
                    foreach (Value v in mTuple)
                    {
                        s += v.ToString();
                        s += ",";
                    }
                    s.TrimEnd(',');
                    s += ")";
                    return s;
                case Type.VOID:
                    return "void";
                default:
                    return "{" + typ.ToString() + "}";
                }
            }
        }

        public void assertType(ASTNode n, string s, Type t, Value v)
        {
            assertType(n.mToken.Key, s, t, v);
        }

        public void assertType(string n, string s, Type t, Value v)
        {
            if (v.typ != t)
                throw new EvalError(n + ": expected " + t.ToString() + ", " + s + " is " + v.typ.ToString() + " " + v.ToString());
        }

        public Value exec(string name, Value arglist, Processor p)
        {
            List<Value> args = arglist.l;
            var match = System.Text.RegularExpressions.Regex.Match(name, "^(.+)\\.([^.]+)");
            string error;
            if (match.Success)
            {
                string mainName = match.Groups[1].Value;
                string method = match.Groups[2].Value;
                if (p.outputs.ContainsKey(mainName))
                {
                    IOutputData output = p.outputs[mainName];
                    if (method.Equals("set"))
                    {
                        output.setValue(arglist);
                        return new Value();
                    }
                    if (method.Equals("incr") && arglist.typ == Type.DOUBLE)
                    {
                        output.slewValue(arglist);
                        return new Value();
                    }
                    if (method.Equals("decr") && arglist.typ == Type.DOUBLE)
                    {
                        output.slewValue(new Value(-arglist.d));
                        return new Value();
                    }
                    error = "no such method";
                }
                else
                {
                    error = "no such output";
                }
            }
            else
            {
                error = "method expected";
            }
            string s = error + ": " + name + ":";
            foreach (Value arg in args)
                s += " " + arg.ToString();
            throw new ExecError(s);
        }

        public Value evalRecursive(ASTNode n, Processor p)
        {
            Value left, right;
            switch(n.mToken.Value)
            {
            case Tokens.TOK_ARITH_OP: // + - * /
                left = evalRecursive(n.mChildren[0], p);
                right = evalRecursive(n.mChildren[1], p);
                assertType(n, "left", Type.DOUBLE, left);
                assertType(n, "right", Type.DOUBLE, right);
                if (n.mToken.Key.Equals("+"))
                    return new Value(left.d + right.d);
                else if (n.mToken.Key.Equals("-"))
                    return new Value(left.d - right.d);
                else if (n.mToken.Key.Equals("*"))
                    return new Value(left.d * right.d);
                else if (n.mToken.Key.Equals("/"))
                    return new Value(left.d / right.d);
                else // can't happen
                    throw new EvalError(n.mToken.Key);
            case Tokens.TOK_AT:
                left = evalRecursive(n.mChildren[0], p);
                right = evalRecursive(n.mChildren[1], p);
                assertType(n, "left", Type.NAME, left);
                return exec(left.n, right, p);
            case Tokens.TOK_COMMA:
                left = evalRecursive(n.mChildren[0], p);
                right = evalRecursive(n.mChildren[1], p);
                return new Value(left, right);
            case Tokens.TOK_COMP_OP: // < >
                left = evalRecursive(n.mChildren[0], p);
                right = evalRecursive(n.mChildren[1], p);
                assertType(n, "left", Type.DOUBLE, left);
                assertType(n, "right", Type.DOUBLE, right);
                if (n.mToken.Key.Equals("<"))
                    return new Value(left.d < right.d);
                else if (n.mToken.Key.Equals(">"))
                    return new Value(left.d > right.d);
                else // can't happen
                    throw new EvalError(n.mToken.Key);
            case Tokens.TOK_IDENT:
                if (n.mToken.Key.Equals("true"))
                    return new Value(true);
                if (n.mToken.Key.Equals("false"))
                    return new Value(false);
                if (p.inputValues.ContainsKey(n.mToken.Key))
                {
                    InputValue i = p.inputValues[n.mToken.Key];
                    if (i.typ == InputType.BOOLEAN)
                        return new Value(i.Bool);
                    if (i.typ == InputType.DOUBLE)
                        return new Value(i.Double);
                    return new Value();
                }
                return new Value(n.mToken.Key);
            case Tokens.TOK_KEYWORD: // ON DO IF THEN
                if (n.mToken.Key.Equals("ON"))
                {
                    Value cond = evalRecursive(n.mChildren[0], p);
                    assertType(n, "cond", Type.BOOLEAN, cond);
                    if (cond.b && !lastValue)
                    {
                        //Logging.Log("edge fired! " + mText);
                        if (TimeWarp.CurrentRateIndex > 0)
                            TimeWarp.SetRate(0, true); // force 1x speed
                        try
                        {
                            evalRecursive(n.mChildren[1], p);
                        }
                        catch(Exception ex)
                        {
                            Logging.Log(ex.ToString());
                        }
                    }
                    lastValue = cond.b;
                    return new Value();
                }
                if (n.mToken.Key.Equals("IF"))
                {
                    Value cond = evalRecursive(n.mChildren[0], p);
                    assertType(n, "cond", Type.BOOLEAN, cond);
                    if (cond.b)
                    {
                        evalRecursive(n.mChildren[1], p);
                    }
                    return new Value();
                }
                if (n.mToken.Key.Equals("DO"))
                    return evalRecursive(n.mChildren[0], p);
                if (n.mToken.Key.Equals("THEN"))
                    return evalRecursive(n.mChildren[0], p);
                // can't happen
                throw new EvalError(n.mToken.Key);
            case Tokens.TOK_LITERAL:
                try
                {
                    return new Value(Double.Parse(n.mToken.Key));
                }
                catch (Exception) // can't happen?
                {
                    throw new EvalError(n.mToken.Key);
                }
            case Tokens.TOK_LOG_OP: // AND OR
                left = evalRecursive(n.mChildren[0], p);
                right = evalRecursive(n.mChildren[1], p);
                assertType(n, "left", Type.BOOLEAN, left);
                assertType(n, "right", Type.BOOLEAN, right);
                if (n.mToken.Key.Equals("AND"))
                    return new Value(left.b && right.b);
                else if (n.mToken.Key.Equals("OR"))
                    return new Value(left.b || right.b);
                else // can't happen
                    throw new EvalError(n.mToken.Key);
            case Tokens.TOK_SEMI:
                evalRecursive(n.mChildren[0], p);
                evalRecursive(n.mChildren[1], p);
                return new Value();
            case Tokens.TOK_UN_OP: // !
                if (!n.mToken.Key.Equals("!")) // can't happen
                    throw new EvalError(n.mToken.Key);
                left = evalRecursive(n.mChildren[0], p);
                assertType(n, "child", Type.BOOLEAN, left);
                return new Value(!left.b);
            default: // can't happen
                throw new EvalError(n.mToken.Value.ToString());
            }
        }

        public void eval(Processor p)
        {
            if (!skip)
            {
                try
                {
                    evalRecursive(mAST, p);
                }
                catch (Instruction.EvalError exc)
                {
                    Logging.Log("EvalError: " + exc.ToString());
                    skip = true;
                }
                catch (Instruction.ExecError exc)
                {
                    Logging.Log("ExecError: " + exc.ToString());
                    skip = true;
                }
            }
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
        private Processor mProc = null;

        private Vessel parentVessel { get { return mProc.parentVessel; } }

        public Batteries (Processor p)
        {
            mProc = p;
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

    public class VesselTMR : IInputData
    {
        public string name { get { return "vesselTmr"; } }
        public bool available { get { return parentVessel != null && TotalMass > 0.1f; }}
        public InputType typ {get { return InputType.DOUBLE; } }
        public InputValue value { get { return new InputValue(TMR); } }
        private Processor mProc = null;

        private Vessel parentVessel { get { return mProc.parentVessel; } }

        public VesselTMR (Processor p)
        {
            mProc = p;
        }

        private double TotalThrust
        {
            get { return FlightCore.GetTotalThrust(parentVessel); }
        }

        private double TotalMass
        {
            get { return parentVessel.GetTotalMass(); }
        }

        private double TMR
        {
            get { return parentVessel != null && TotalMass > 0.1f ? TotalThrust / TotalMass : Double.PositiveInfinity; }
        }

    }

    public class SensorDriven
    {
        public virtual string name { get { return "abstract"; } }
        private Processor mProc = null;
        public Vessel parentVessel { get { return mProc.parentVessel; } }
        public SensorDriven (Processor p)
        {
            mProc = p;
        }
        public bool available
        {
            get
            {
                return parentVessel != null && parentVessel.Parts.Any(p => p.FindModulesImplementing<KPU.Modules.ModuleKpuSensor>().Any(m => m.sensorType.Equals(name)));
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

        public Gear (Processor p) : base(p)
        {
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

        public SrfHeight (Processor p) : base(p)
        {
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

        public SrfSpeed (Processor p) : base(p)
        {
        }
    }

    public class SrfVerticalSpeed : SensorDriven, IInputData
    {
        public override string name { get { return "srfVerticalSpeed"; } }
        public InputType typ { get { return InputType.DOUBLE; } }
        public InputValue value
        {
            get
            {
                if (!available) return new InputValue(Double.PositiveInfinity);
                Vector3 v = parentVessel.GetSrfVelocity();
                Vector3 up = Vector3.Normalize(parentVessel.CoM - parentVessel.mainBody.position);
                double vs = Vector3.Dot(v, up);
                return new InputValue(Math.Round(vs / res) * res);
            }
        }

        public SrfVerticalSpeed (Processor p) : base(p)
        {
        }
    }

    public class LocalG : SensorDriven, IInputData
    {
        public override string name { get { return "localGravity"; } }
        public InputType typ {get { return InputType.DOUBLE; } }
        public InputValue value { get { return new InputValue(FlightGlobals.getGeeForceAtPosition(FlightGlobals.ship_position).magnitude); } }

        public LocalG (Processor p) : base(p)
        {
        }
    }

    public interface IOutputData
    {
        string name { get; }
        Instruction.Type typ { get; }
        Instruction.Value value { get; }
        void Invoke(FlightCtrlState fcs, Processor p);
        void clean();
        void setValue(Instruction.Value value);
        void slewValue(Instruction.Value rate);
    }

    public class Throttle : IOutputData
    {
        public string name { get { return "throttle"; } }
        public Instruction.Type typ {get { return Instruction.Type.DOUBLE; } }
        private double mValue = 0;
        private double mSlewRate = 0;
        public Instruction.Value value { get { return new Instruction.Value(mValue); } }

        public void Invoke(FlightCtrlState fcs, Processor p)
        {
            setTo(mValue + mSlewRate * TimeWarp.deltaTime);
            fcs.mainThrottle = (float)mValue / 100.0f;
            mSlewRate = 0;
        }

        public void clean()
        {
            mValue = 0;
            mSlewRate = 0;
        }

        private void setTo(double value)
        {
            mValue = Math.Min(Math.Max(value, 0), 100.0f);
        }

        public void setValue(Instruction.Value value)
        {
            if (value.typ == Instruction.Type.DOUBLE)
            {
                setTo(value.d);
            }
        }

        public void slewValue(Instruction.Value rate)
        {
            if (rate.typ == Instruction.Type.DOUBLE)
            {
                mSlewRate += rate.d;
            }
        }
    }

    public class Orient : IOutputData
    {
        public string name { get { return "orient"; } }
        public Instruction.Type typ {get { return Instruction.Type.NAME; } }
        private string mValue = "none";
        public Instruction.Value value { get { return new Instruction.Value(mValue); } }

        public void Invoke(FlightCtrlState fcs, Processor p)
        {
            if (!mValue.Equals("none"))
                FlightCore.HoldAttitude(fcs, p, new FlightAttitude(mValue));
        }

        public void clean()
        {
            mValue = "none";
        }

        public void setValue(Instruction.Value value)
        {
            if (value.typ == Instruction.Type.NAME)
            {
                mValue = value.n;
            }
        }

        public void slewValue(Instruction.Value rate)
        {
        }
    }

    public class GearOutput : IOutputData
    {
        public string name { get { return "gear"; } }
        public Instruction.Type typ {get { return Instruction.Type.BOOLEAN; } }
        private bool mValue = false;
        private bool mChange = false;
        public Instruction.Value value { get { return new Instruction.Value(mValue); } }

        public void Invoke(FlightCtrlState fcs, Processor p)
        {
            if (mChange)
                p.parentVessel.ActionGroups.SetGroup(KSPActionGroup.Gear, mValue);
            mChange = false;
        }

        public void clean()
        {
            mValue = false;
            mChange = false;
        }

        public void setValue(Instruction.Value value)
        {
            if (value.typ == Instruction.Type.BOOLEAN)
            {
                mValue = value.b;
                mChange = true;
            }
        }

        public void slewValue(Instruction.Value rate)
        {
        }
    }

    public class Processor
    {
        public bool hasLevelTrigger, hasLogicOps, hasArithOps;
        public int imemWords;
        public List<Instruction> instructions;

        private Part mPart;
        private bool mIsRunning;
        public bool isRunning
        {
            get { return mIsRunning; }
            set {
                mIsRunning = value;
                foreach (IOutputData o in outputs.Values)
                {
                    o.clean();
                }
            }
        }

        // Warning, may be null
        public Vessel parentVessel { get { return mPart.vessel; } }

        private List<IInputData> inputs = new List<IInputData>();
        public Dictionary<string, IOutputData> outputs = new Dictionary<string, IOutputData>();

        private void addOutput(IOutputData o)
        {
            outputs[o.name] = o;
        }

        public Processor (Part part, Modules.ModuleKpuProcessor module)
        {
            mPart = part;
            hasLevelTrigger = module.hasLevelTrigger;
            hasLogicOps = module.hasLogicOps;
            hasArithOps = module.hasArithOps;
            imemWords = module.imemWords;
            isRunning = module.isRunning;
            instructions = new List<Instruction>();
            inputs.Add(new Batteries(this));
            inputs.Add(new Gear(this));
            inputs.Add(new SrfHeight(this));
            inputs.Add(new SrfSpeed(this));
            inputs.Add(new SrfVerticalSpeed(this));
            inputs.Add(new VesselTMR(this));
            inputs.Add(new LocalG(this));
            addOutput(new Throttle());
            addOutput(new Orient());
            addOutput(new GearOutput());

            initPIDParameters();
        }

        public void ClearInstructions()
        {
            foreach (Instruction i in instructions)
            {
                imemWords += i.imemWords;
            }
            instructions.Clear();
        }

        public bool AddInstruction(string text)
        {
            return AddInstruction(text, false, false);
        }

        public bool AddInstruction(string text, bool lastValue, bool skip)
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
            //Logging.Log("Added instruction: " + i.mText);
            return true;
        }

        public Dictionary<string, InputValue> inputValues;

        public void OnUpdate ()
        {
            inputValues = new Dictionary<string, InputValue>();
            foreach (IInputData i in inputs)
            {
                try
                {
                    if (i.available)
                    {
                        inputValues.Add(i.name, i.value);
                    }
                }
                catch (Exception exc)
                {
                    Logging.Log("Problem in input " + i.name);
                    Logging.Log(exc.ToString());
                    Logging.Log(exc.StackTrace);
                }
            }
            if (isRunning)
            {
                foreach (Instruction i in instructions)
                {
                    i.eval(this);
                }
            }
        }

        public void OnFlyByWire (FlightCtrlState fcs)
        {
            if (isRunning)
            {
                OnUpdate(); // ensure outputs are up-to-date
                foreach (IOutputData o in outputs.Values)
                {
                    o.Invoke(fcs, this);
                }
            }
        }

        public void OnFixedUpdate()
        {
            updatePIDParameters();
        }

        public PIDController pid { get; private set; }
        public Vector3d lastAct { get; set; }
        public double Tf = 0.3;
        public double TfMin = 0.1;
        public double TfMax = 0.5;
        public double kpFactor = 3;
        public double kiFactor = 6;
        public double kdFactor = 0.5;

        public void initPIDParameters()
        {
            pid = new PIDController(0, 0, 0, 1, -1);
            pid.Kd = kdFactor / Tf;
            pid.Kp = pid.Kd / (kpFactor * Math.Sqrt(2) * Tf);
            pid.Ki = pid.Kp / (kiFactor * Math.Sqrt(2) * Tf);
            pid.intAccum = Vector3.ClampMagnitude(pid.intAccum, 5);
            lastAct = Vector3d.zero;
        }

        // Calculations of Tf are not safe during Processor constructor
        // Probably because the ship is only half-initialized...
        public void updatePIDParameters()
        {
            if (parentVessel != null)
            {
                Vector3d torque = SteeringHelper.GetTorque(parentVessel,
                    parentVessel.ctrlState != null ? parentVessel.ctrlState.mainThrottle : 0.0f);
                var CoM = parentVessel.findWorldCenterOfMass();
                var MoI = parentVessel.findLocalMOI(CoM);

                Vector3d ratio = new Vector3d(
                                 torque.x != 0 ? MoI.x / torque.x : 0,
                                 torque.y != 0 ? MoI.y / torque.y : 0,
                                 torque.z != 0 ? MoI.z / torque.z : 0
                             );

                Tf = Mathf.Clamp((float)ratio.magnitude / 20f, 2 * TimeWarp.fixedDeltaTime, 1f);
                Tf = Mathf.Clamp((float)Tf, (float)TfMin, (float)TfMax);
            }
        }

        public void Save (ConfigNode node)
        {
            if (node.HasNode("Processor"))
                node.RemoveNode("Processor");
            
            ConfigNode Proc = new ConfigNode("Processor");

            if (pid != null)
                pid.Save(Proc);

            ConfigNode InstList = new ConfigNode("Instructions");
            foreach (Instruction i in instructions)
            {
                ConfigNode Inst = new ConfigNode("Instruction");
                Inst.AddValue("code", i.mText);
                Inst.AddValue("lastValue", i.lastValue);
                Inst.AddValue("skip", i.skip);
                InstList.AddNode(Inst);
            }

            Proc.AddNode(InstList);

            node.AddNode(Proc);
        }

        public void Load (ConfigNode node)
        {
            if (!node.HasNode("Processor"))
                return;
            
            ConfigNode Proc = node.GetNode("Processor");

            if (pid != null)
                pid.Load(Proc);

            ConfigNode InstList = Proc.GetNode("Instructions");
            if (InstList != null)
            {
                ClearInstructions();
                foreach (ConfigNode Inst in InstList.GetNodes("Instruction"))
                {
                    string code = Inst.GetValue("code");
                    if (code != null)
                    {
                        bool lastValue = false, skip = false;
                        bool.TryParse(Inst.GetValue("lastValue"), out lastValue);
                        bool.TryParse(Inst.GetValue("skip"), out skip);
                        AddInstruction(code, lastValue, skip);
                    }
                }
            }
        }
    }
}

