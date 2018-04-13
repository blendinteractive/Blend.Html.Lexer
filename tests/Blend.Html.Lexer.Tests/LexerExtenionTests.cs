using System.Linq;
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

        [Fact]
        public void InvalidHtmlCanBeReproduced()
        {
            const string html = "<a id=\"space here is invalid\" Link</a>";
            string result = html.ReplaceElement(x => false, "Replaced!");
            Assert.Equal(html, result);
        }

        [Fact]
        public void InvalidHtmlCanBeReproduced2()
        {
            const string html = "<a id=\"forgot to close this>Link</a>";
            string result = html.ReplaceElement(x => false, "Replaced!");
            Assert.Equal(html, result);
        }

        [Fact]
        public void CanExtractChunk()
        {
            const string html = "<body><div id=\"extract\">Extract Me</div></body>";
            var extractedContents = HtmlLexer
                .Read(html)
                .WithInElement(x => x.IsNamed("div") && x.AttributeIs("id", "extract"))
                .Where(x => x.WithinElement)
                .Select(x => x.ElementEvent)
                .ToList();

            Assert.Equal(3, extractedContents.Count);
            Assert.True(extractedContents[0].Fragment.IsNamed("div"));
            Assert.Equal("Extract Me", extractedContents[1].Fragment.Value);
            Assert.Equal(DomElementEventType.Pop, extractedContents[2].Type);
        }
    }
}
