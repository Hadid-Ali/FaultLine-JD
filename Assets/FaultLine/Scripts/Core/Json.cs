// Minimal JSON reader/writer (ordered objects). Pure C#, no Unity dependency.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace FaultLine
{
    public class JObj : List<KeyValuePair<string, object>>
    {
        public object this[string k]
        {
            get { for (int i = 0; i < Count; i++) if (base[i].Key == k) return base[i].Value; return null; }
            set
            {
                for (int i = 0; i < Count; i++) if (base[i].Key == k) { base[i] = new KeyValuePair<string, object>(k, value); return; }
                Add(new KeyValuePair<string, object>(k, value));
            }
        }
        public bool Has(string k) { for (int i = 0; i < Count; i++) if (base[i].Key == k) return true; return false; }
        public void Remove(string k) { for (int i = 0; i < Count; i++) if (base[i].Key == k) { RemoveAt(i); return; } }
        public IEnumerable<string> Keys { get { foreach (var kv in this) yield return kv.Key; } }
        public JObj O(string k) { return this[k] as JObj; }
        public List<object> A(string k) { return this[k] as List<object>; }
        public string S(string k, string d = null) { var v = this[k]; return v == null ? d : v as string ?? Convert.ToString(v, CultureInfo.InvariantCulture); }
        public double D(string k, double d = 0) { var v = this[k]; if (v is double x) return x; if (v is long l) return l; if (v is int i) return i; if (v is bool b) return b ? 1 : 0; return d; }
        public int I(string k, int d = 0) { return this[k] == null ? d : (int)D(k, d); }
        public bool B(string k, bool d = false) { var v = this[k]; if (v is bool b) return b; if (v == null) return d; return D(k) != 0; }
    }

    public static class Json
    {
        public static object Parse(string s) { int i = 0; var v = Val(s, ref i); return v; }
        static void Ws(string s, ref int i) { while (i < s.Length && (s[i] == ' ' || s[i] == '\n' || s[i] == '\r' || s[i] == '\t')) i++; }
        static object Val(string s, ref int i)
        {
            Ws(s, ref i);
            char c = s[i];
            if (c == '{')
            {
                var o = new JObj(); i++; Ws(s, ref i);
                if (s[i] == '}') { i++; return o; }
                while (true)
                {
                    Ws(s, ref i); string k = Str(s, ref i); Ws(s, ref i); i++; // :
                    var v = Val(s, ref i); o.Add(new KeyValuePair<string, object>(k, v)); Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; return o;
                }
            }
            if (c == '[')
            {
                var a = new List<object>(); i++; Ws(s, ref i);
                if (s[i] == ']') { i++; return a; }
                while (true)
                {
                    a.Add(Val(s, ref i)); Ws(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; return a;
                }
            }
            if (c == '"') return Str(s, ref i);
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (s.Length - i >= 5 && string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (s.Length - i >= 4 && string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            int st = i; while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(st, i - st), CultureInfo.InvariantCulture);
        }
        static string Str(string s, ref int i)
        {
            var sb = new StringBuilder(); i++;
            while (s[i] != '"')
            {
                char c = s[i++];
                if (c == '\\')
                {
                    char e = s[i++];
                    switch (e)
                    {
                        case 'n': sb.Append('\n'); break; case 't': sb.Append('\t'); break; case 'r': sb.Append('\r'); break;
                        case 'b': sb.Append('\b'); break; case 'f': sb.Append('\f'); break;
                        case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; break;
                        default: sb.Append(e); break;
                    }
                }
                else sb.Append(c);
            }
            i++; return sb.ToString();
        }
        public static string Write(object v) { var sb = new StringBuilder(); W(sb, v); return sb.ToString(); }
        static void W(StringBuilder sb, object v)
        {
            switch (v)
            {
                case null: sb.Append("null"); break;
                case string s: Q(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case JObj o:
                    sb.Append('{'); bool f = true;
                    foreach (var kv in o) { if (!f) sb.Append(','); f = false; Q(sb, kv.Key); sb.Append(':'); W(sb, kv.Value); }
                    sb.Append('}'); break;
                case System.Collections.IList l:
                    sb.Append('['); for (int i = 0; i < l.Count; i++) { if (i > 0) sb.Append(','); W(sb, l[i]); }
                    sb.Append(']'); break;
                case double d: sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); break;
                case float fl: sb.Append(((double)fl).ToString("R", CultureInfo.InvariantCulture)); break;
                case int n: sb.Append(n.ToString(CultureInfo.InvariantCulture)); break;
                case long n2: sb.Append(n2.ToString(CultureInfo.InvariantCulture)); break;
                default: Q(sb, v.ToString()); break;
            }
        }
        static void Q(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break; case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break; case '\r': sb.Append("\\r"); break; case '\t': sb.Append("\\t"); break;
                    default: if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4")); else sb.Append(c); break;
                }
            }
            sb.Append('"');
        }
    }
}
