using System.Text;
using Xunit;

namespace Blend.Html.Lexer.Tests
{
    public class LexerExtenionTests
    {
        [Fact]
        public void CanRecreateHtmlFromFragments()
        {
            string original = "<!doctype html><html><body><p>This is a test<p>Bad paragraph</p><!--Comment--><script>alert('<ok   >');</script></body></html>";
            string result = original.ReplaceElement(x => x.IsNamed("no-such-element"), "<!-- nothing is replaced -->");

            Assert.Equal(original, result);
        }

        [Fact]
        public void CanReplaceElements()
        {
            const string html = "<html><nav id=\"replacement\"><b>Replace <i>me</i></b></nav><footer>Leave me</footer></html>";
            string result = html.ReplaceElement((fragment) => fragment.IsOpen("nav") && fragment.HasAttributeValue("id", "replacement"), "<nav>Replaced</nav>");
            Assert.Equal("<html><nav>Replaced</nav><footer>Leave me</footer></html>", result);
        }

        [Fact]
        public void CanReplaceElements2()
        {
            const string html = "<html><body><header>Head!</header><nav class=\"primary\">TO REPLACE</nav></body></html>";
            string result = html.ReplaceElement(x => x.IsOpen("nav"), "<nav>Replaced!</nav>");
            Assert.Equal("<html><body><header>Head!</header><nav>Replaced!</nav></body></html>", result);
        }
    }
}
