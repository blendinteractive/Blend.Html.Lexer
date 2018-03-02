using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Blend.Html.Lexer.Tests
{
    static class Extensions
    {
        public static void AssertParsesTo(this string rawHtml, params Fragment[] fragments)
        {
            var result = HtmlLexer.Read(rawHtml).ToArray();
            Assert.Equal(fragments.Length, result.Length);
            for (int x = 0; x < fragments.Length; x++)
                Assert.Equal(fragments[x], result[x]);
        }

        public static void AssertRoundTripsTo(this string rawHtml, string expectedHtml)
        {
            var actualHtml = LexedDomParser.Execute(rawHtml).WriteHtml();
            Assert.Equal(expectedHtml, actualHtml);
        }
    }

    public static class ToStringDomParser
    {
        public static string WriteHtml(this IEnumerable<DomElementEvent> parseEvents)
        {
            Stack<Fragment> stack = new Stack<Fragment>();
            StringBuilder sb = new StringBuilder();

            foreach (var ev in parseEvents)
            {
                switch (ev.Type)
                {
                    case DomElementEventType.Child:
                        sb.Append(ev.Fragment.ToString());
                        break;
                    case DomElementEventType.Push:
                        sb.Append(ev.Fragment.ToString());
                        stack.Push(ev.Fragment);
                        break;
                    case DomElementEventType.Pop:
                        var popped = stack.Pop();
                        var closeFragment = ev.Fragment ?? popped.AsCloseFragment();
                        sb.Append(closeFragment.ToString());
                        break;
                }
            }

            return sb.ToString();
        }
    }
}
