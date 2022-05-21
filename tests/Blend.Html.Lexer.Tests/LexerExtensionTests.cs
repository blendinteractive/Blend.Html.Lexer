using System;
using System.Linq;
using System.Text;
using Xunit;

namespace Blend.Html.Lexer.Tests
{
    public class LexerExtensionTests
    {
        [Fact]
        public void CanRecreateHtmlFromFragments()
        {
            string original = "<!doctype html><html><body><p>This is a test<p>Bad paragraph</p><!--Comment--><script>alert('<ok   >');</script></body></html>";
            string result = original.ReplaceElements(x => x.IsNamed("no-such-element"), "<!-- nothing is replaced -->", NodeType.OuterNode);
            Assert.Equal(original, result);
        }

        [Fact]
        public void CanReplaceElements()
        {
            const string html = "<html><nav id=\"replacement\"><b>Replace <i>me</i></b></nav><footer>Leave me</footer></html>";
            string result = html.ReplaceElements((fragment) => fragment.IsOpen("nav") && fragment.HasAttributeValue("id", "replacement"), "<nav>Replaced</nav>", NodeType.OuterNode);
            Assert.Equal("<html><nav>Replaced</nav><footer>Leave me</footer></html>", result);
        }

        [Fact]
        public void CanReplaceElements2()
        {
            const string html = "<html><body><header>Head!</header><nav class=\"primary\">TO REPLACE</nav></body></html>";
            string result = html.ReplaceElements(x => x.IsOpen("nav"), "<nav>Replaced!</nav>", NodeType.OuterNode);
            Assert.Equal("<html><body><header>Head!</header><nav>Replaced!</nav></body></html>", result);
        }

        [Fact]
        public void CanReplaceOnlyInnerNodes()
        {
            const string html = "<html><body><header>Head!</header><nav class=\"primary\">TO REPLACE</nav></body></html>";
            string result = html.ReplaceElements(x => x.IsOpen("nav"), "<p>Replaced!</p>", NodeType.InnerNode);
            Assert.Equal("<html><body><header>Head!</header><nav class=\"primary\"><p>Replaced!</p></nav></body></html>", result);
        }

        [Fact]
        public void InvalidHtmlCanBeReproduced()
        {
            const string html = "<a id=\"space here is invalid\" Link</a>";
            string result = html.ReplaceElements(x => false, "Replaced!", NodeType.OuterNode);
            Assert.Equal(html, result);
        }

        [Fact]
        public void InvalidHtmlCanBeReproduced2()
        {
            const string html = "<a id=\"forgot to close this>Link</a>";
            string result = html.ReplaceElements(x => false, "Replaced!", NodeType.OuterNode);
            Assert.Equal(html, result);
        }

        [Fact]
        public void CanExtractChunk()
        {
            const string html = "<body><div id=\"extract\">Extract Me</div></body>";
            var extractedContents = HtmlLexer
                .Read(html)
                .WithInElement(x => x.IsNamed("div") && x.AttributeIs("id", "extract"), true)
                .Where(x => x.WithinElement)
                .Select(x => x.ElementEvent)
                .ToList();

            Assert.Equal(3, extractedContents.Count);
            Assert.True(extractedContents[0].Fragment.IsNamed("div"));
            Assert.Equal("Extract Me", extractedContents[1].Fragment.Value);
            Assert.Equal(DomElementEventType.Pop, extractedContents[2].Type);
        }

        [Fact]
        public void CanExtractChunkWithExtensionMethod()
        {
            const string html = "<body><div id=\"extract\">Extract Me</div></body>";
            var actual = html.ExtractElements(x => x.IsNamed("div") && x.AttributeIs("id", "extract"), NodeType.OuterNode);
            Assert.Equal("<div id=\"extract\">Extract Me</div>", actual);
        }

        [Fact]
        public void CanExtractChunkListWithExtensionMethod()
        {
            const string html = "<body><section>One</section><section>Two</section></body>";
            var actual = html.ExtractElementsList(x => x.IsNamed("section"), NodeType.InnerNode).ToList();
            Assert.Equal(2, actual.Count);
            Assert.Equal("One", actual[0]);
            Assert.Equal("Two", actual[1]);
        }

        [Fact]
        public void CanExtractChunkListWithExtensionMethodOuterNode()
        {
            const string html = "<body><section>One</section><section>Two</section></body>";
            var actual = html.ExtractElementsList(x => x.IsNamed("section"), NodeType.OuterNode).ToList();
            Assert.Equal(2, actual.Count);
            Assert.Equal("<section>One</section>", actual[0]);
            Assert.Equal("<section>Two</section>", actual[1]);
        }

        [Fact]
        public void CanExtractTextWithExtensionMethod()
        {
            const string html = "<body><div id=\"extract\"><span>Extract</span> <em>Me</em></div></body>";
            var actual = html.ExtractText(x => x.IsNamed("div") && x.AttributeIs("id", "extract"));
            Assert.Equal("Extract Me", actual);
        }

        [Fact]
        public void CanExtractTextListWithExtensionMethod()
        {
            const string html = "<body><p>First</p><p><bold>Second</bold> example</p></body>";
            var actual = html.ExtractTextList(x => x.IsNamed("p")).ToList();
            Assert.Equal(2, actual.Count);
            Assert.Equal("First", actual[0]);
            Assert.Equal("Second example", actual[1]);
        }

        [Fact]
        public void CanExtracInnerChunkWithExtensionMethod()
        {
            const string html = "<body><div id=\"extract\">Extract Me</div></body>";
            var actual = html.ExtractElements(x => x.IsNamed("div") && x.AttributeIs("id", "extract"), NodeType.InnerNode);
            Assert.Equal("Extract Me", actual);
        }

        [Fact]
        public void CanWrapElementsInnerWrapper()
        {
            const string html = "<body><div class=\"wrap-me\"><p>This should be wrapped</p></div></body>";
            var updatedContent = html.WrapElements(fragment => fragment.IsNamed("div") && fragment.AttributeIs("class", "wrap-me"), "<span class=\"wrapped\">", "</span>", WrapElementsType.AddInnerWrapper);
            Assert.Equal("<body><div class=\"wrap-me\"><span class=\"wrapped\"><p>This should be wrapped</p></span></div></body>", updatedContent);
        }

        [Fact]
        public void CanWrapElementsOuterWrapper()
        {
            const string html = "<body><div class=\"wrap-me\"><p>This should be wrapped</p></div></body>";
            var updatedContent = html.WrapElements(fragment => fragment.IsNamed("div") && fragment.AttributeIs("class", "wrap-me"), "<span class=\"wrapped\">", "</span>", WrapElementsType.AddOuterWrapper);
            Assert.Equal("<body><span class=\"wrapped\"><div class=\"wrap-me\"><p>This should be wrapped</p></div></span></body>", updatedContent);
        }

        [Fact]
        public void CanWrapElementsReplaced()
        {
            const string html = "<body><div class=\"wrap-me\"><p>This should be wrapped</p></div></body>";
            var updatedContent = html.WrapElements(fragment => fragment.IsNamed("div") && fragment.AttributeIs("class", "wrap-me"), "<span class=\"wrapped\">", "</span>", WrapElementsType.ReplaceMatchedElements);
            Assert.Equal("<body><span class=\"wrapped\"><p>This should be wrapped</p></span></body>", updatedContent);
        }

        [Fact]
        public void CrapWrapMultiple()
        {
            int currentCount = 0;

            const string html = "<ul><li>One</li><li>Two</li></ul>";
            var updatedContent = html.WrapElements(fragment => fragment.IsNamed("li"), 
                () => $"<span id=\"t{currentCount++}\">",
                () => "</span>",
                WrapElementsType.AddInnerWrapper);
            Assert.Equal("<ul><li><span id=\"t0\">One</span></li><li><span id=\"t1\">Two</span></li></ul>", updatedContent);
        }


        [Fact]
        public void CrapProcessNodes()
        {
            const string html = "<ul><li>One</li><li>Two</li></ul>";
            var events = HtmlLexer.Read(html).WithInElement(ev => ev.IsNamed("li"), false);
            
            StringBuilder sb = new StringBuilder();
            int count = 0;

            events.ProcessElements(
                outside: (ev) =>
                {
                    // Called for every node that is "outside" the matched node
                    if (ev.ElementEvent.Fragment != null)
                    {
                        sb.Append(html, ev.ElementEvent.Fragment.Trivia.StartPosition, ev.ElementEvent.Fragment.Trivia.Length);
                    }
                },
                onEnter: (ev) => sb.Append($"<li id=\"t{count++}\"><b>"), // Called at the opening of the matched node(s)
                onExit: (ev) => sb.Append($"</b><!-- {count} --></li>"), // Call at the close of the matched node(s)
                inside: (ev) =>
                {
                    // Called for every fragement within the matched node
                    if (ev.ElementEvent.Fragment != null)
                    {
                        sb.Append(html, ev.ElementEvent.Fragment.Trivia.StartPosition, ev.ElementEvent.Fragment.Trivia.Length);
                    }
                }
            );

            var updatedContent = sb.ToString();
            Assert.Equal("<ul><li id=\"t0\"><b>One</b><!-- 1 --></li><li id=\"t1\"><b>Two</b><!-- 2 --></li></ul>", updatedContent);
        }
    }
}
