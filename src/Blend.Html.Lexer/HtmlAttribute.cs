namespace Blend.Html.Lexer
{
    public class HtmlAttribute
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public override string ToString() => Value == null ? Key : $"{Key}=\"{Value}\"";

        public override bool Equals(object obj)
        {

            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var casted = (HtmlAttribute)obj;
            return casted.Key == Key && casted.Value == Value;

        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 13;
                if (Key != null)
                    hash = (hash * 7) + Key.GetHashCode();
                if (Value != null)
                    hash = (hash * 7) + Value.GetHashCode();
                return hash;
            }
        }

        public static HtmlAttribute Create(string key, string value = null) => new HtmlAttribute { Key = key, Value = value };
    }
}
