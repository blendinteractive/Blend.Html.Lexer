using System;
using System.Collections.Generic;
using System.Linq;

namespace Blend.Html.Lexer
{
    public class Fragment
    {
        public FragmentTriva Trivia { get; } = new FragmentTriva();
        public FragmentType FragmentType { get; set; }
        public string Value { get; set; }
        public bool IsSelfClosing { get; set; }
        public HtmlAttribute[] Attributes { get; set; }

        public override string ToString()
        {
            switch (FragmentType)
            {
                case FragmentType.Open:
                    string attributes = "";
                    if (Attributes != null && Attributes.Any())
                    {
                        attributes = " " + string.Join(" ", Attributes.Select(x => x.ToString()));
                    }
                    string closing = IsSelfClosing ? "/" : "";

                    return $"<{Value}{attributes}{closing}>";
                case FragmentType.Close: return $"</{Value}>";
                case FragmentType.Comment: return $"<!--{Value}-->";
                case FragmentType.Text: return $"{Value}";
                case FragmentType.Doctype: return $"<!DOCTYPE {Value}>";
                default: throw new NotImplementedException();
            }
        }

        Dictionary<string, HtmlAttribute> lookup = null;
        void EnsureAttributeLookup()
        {
            if (lookup != null)
                return;

            lookup = new Dictionary<string, HtmlAttribute>();
            if (Attributes != null)
            {
                foreach (var attr in Attributes)
                    lookup[attr.Key] = attr;
            }
        }

        public HtmlAttribute this[string attributeName]
        {
            get
            {
                EnsureAttributeLookup();
                return lookup.TryGetValue(attributeName, out HtmlAttribute result) ? result : null;
            }
        }

        public string GetAttributeValue(string key, string defaultValue = null)
        {
            var attr = this[key];
            return attr == null ? defaultValue : attr.Value;
        }

        public bool HasAttribute(string key)
        {
            EnsureAttributeLookup();
            return lookup.ContainsKey(key);
        }

        public bool HasAttributeValue(string key, string value)
        {
            var hasAttribute = HasAttribute(key);
            return string.Compare(GetAttributeValue(key), value, true) == 0;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var casted = (Fragment)obj;
            if (casted.FragmentType != FragmentType
                || casted.IsSelfClosing != casted.IsSelfClosing
                || casted.Value != casted.Value)
                return false;

            var cattrs = casted.Attributes ?? Enumerable.Empty<HtmlAttribute>();
            var mattrs = Attributes ?? Enumerable.Empty<HtmlAttribute>();
            if (!Enumerable.SequenceEqual(cattrs, mattrs))
                return false;

            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 13;
                hash = (hash * 7) + FragmentType.GetHashCode();
                if (Value != null)
                    hash = (hash * 7) + Value.GetHashCode();
                if (Attributes != null)
                    hash = (hash * 7) + Attributes.GetHashCode();
                hash = (hash * 7) + IsSelfClosing.GetHashCode();
                return hash;
            }
        }

        public static Fragment OpenTag(string name, bool selfClose, params HtmlAttribute[] attrs)
            => new Fragment { FragmentType = FragmentType.Open, IsSelfClosing = selfClose, Attributes = attrs, Value = name };
        public static Fragment CloseTag(string name) => new Fragment { FragmentType = FragmentType.Close, Value = name };
        public static Fragment Comment(string text) => new Fragment { FragmentType = FragmentType.Comment, Value = text };
        public static Fragment Text(string text) => new Fragment { FragmentType = FragmentType.Text, Value = text };
        public static Fragment Doctype(string text) => new Fragment { FragmentType = FragmentType.Doctype, Value = text };
    }
}
