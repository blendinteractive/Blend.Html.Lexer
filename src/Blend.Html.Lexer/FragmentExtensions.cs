using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Blend.Html.Lexer
{
    public static class FragmentExtensions
    {
        public static bool IsNamed(this Fragment fragment, string name)
            => string.Compare(fragment.Value, name, true) == 0;

        public static bool IsOpen(this Fragment fragment, string name = null)
            => (fragment.FragmentType == FragmentType.Open)
                && (name == null || IsNamed(fragment, name));

        public static bool IsClose(this Fragment fragment, string name = null)
            => (fragment.FragmentType == FragmentType.Close)
                && (name == null || IsNamed(fragment, name));

        public static bool AttributeIs(this Fragment fragment, string name, string value)
            => (fragment.FragmentType == FragmentType.Open)
                && fragment.HasAttribute(name)
                && string.Compare(fragment.GetAttributeValue(name), value, true) == 0;

        public static Fragment AsCloseFragment(this Fragment fragment) => Fragment.CloseTag(fragment.Value);
    }
}
