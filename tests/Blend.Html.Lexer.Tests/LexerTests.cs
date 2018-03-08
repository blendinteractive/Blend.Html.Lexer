using System.Linq;
using Xunit;

namespace Blend.Html.Lexer.Tests
{
    public class LexerTests
    {
        [Fact]
        public void CanParseSingleTag()
        {
            "<img />".AssertParsesTo(
                Fragment.OpenTag("img", true)
            );
        }

        [Fact]
        public void CanParseComment()
        {
            "<!-- -- < test! >-- -->".AssertParsesTo(
                Fragment.Comment(" -- < test! >-- ")
            );
        }

        [Fact]
        public void CanParseNamespacedTag()
        {
            "<ns:node />".AssertParsesTo(
                Fragment.OpenTag("ns:node", true)
            );
        }

        [Fact]
        public void CanParseOddTagName()
        {
            "<node-probably_notValid1 />".AssertParsesTo(
                Fragment.OpenTag("node-probably_notValid1", true)
            );
        }

        [Fact]
        public void CanParseAttribute()
        {
            "<p class=\"test\" />".AssertParsesTo(
                Fragment.OpenTag("p", true, HtmlAttribute.Create("class", "test"))
            );
        }

        [Fact]
        public void CanParseSingleQuoteAttribute()
        {
            "<p class='test' />".AssertParsesTo(
                Fragment.OpenTag("p", true, HtmlAttribute.Create("class", "test"))
            );
        }

        [Fact]
        public void CanParseNoQuoteAttribute()
        {
            "<p class=test />".AssertParsesTo(
                Fragment.OpenTag("p", true, HtmlAttribute.Create("class", "test"))
            );
        }

        [Fact]
        public void CanParseAttributeWithNoValue()
        {
            "<p disabled />".AssertParsesTo(
                Fragment.OpenTag("p", true, HtmlAttribute.Create("disabled"))
            );
        }

        [Fact]
        public void CanParseAttributeWithNamespacedValue()
        {
            "<p ns:data-attr=true />".AssertParsesTo(
                Fragment.OpenTag("p", true, HtmlAttribute.Create("ns:data-attr", "true"))
            );
        }

        [Fact]
        public void CanParseAttributeWithSpaces()
        {
            "<p class = \"test\" data-test = ok disabled />".AssertParsesTo(
                Fragment.OpenTag("p", true, HtmlAttribute.Create("class", "test"), HtmlAttribute.Create("data-test", "ok"), HtmlAttribute.Create("disabled"))
            );
        }

        [Fact]
        public void AttributeSpacesAreWeirdlyOptional()
        {
            // This is not valid HTML, but is the kind of thing browsers seem to grudgingly parse
            // So we grudgingly parse it was well
            "<p class=\"test\"alt=\"alt-test\" />".AssertParsesTo(
                Fragment.OpenTag("p", true, HtmlAttribute.Create("class", "test"), HtmlAttribute.Create("alt", "alt-test"))
            );
        }

        [Fact]
        public void CanParseCloseTag()
        {
            "<p></p>".AssertParsesTo(
                Fragment.OpenTag("p", false),
                Fragment.CloseTag("p")
            );
        }

        [Fact]
        public void CanParseInnerText()
        {
            "<p> Testing </p>".AssertParsesTo(
                Fragment.OpenTag("p", false),
                Fragment.Text(" Testing "),
                Fragment.CloseTag("p")
            );
        }

        [Fact]
        public void CanParseInvalidHtml()
        {
            "<p>I <3 html!</p>".AssertParsesTo(
                Fragment.OpenTag("p", false),
                Fragment.Text("I "),
                Fragment.Text("<"),
                Fragment.Text("3 stuff!"),
                Fragment.CloseTag("p")
            );
        }

        [Fact]
        public void CanParseScriptTagWithHtml()
        {
            "<script type=\"text/javascript\">document.write('<p>Test</p>');</script>".AssertParsesTo(
                Fragment.OpenTag("script", false, HtmlAttribute.Create("type", "text/javascript")),
                Fragment.Text("document.write('<p>Test</p>');"),
                Fragment.CloseTag("script")
            );
        }

        [Fact]
        public void CanParseInvalidDOM()
        {
            "<p>test <bold>tada</p>".AssertParsesTo(
                Fragment.OpenTag("p", false),
                Fragment.Text("test "),
                Fragment.OpenTag("bold", false),
                Fragment.Text("tada"),
                Fragment.CloseTag("p")
            );
        }

        [Fact]
        public void CanParseComplex()
        {
            var testHtml = "<b class=test class=\"test2 again\" class='kjflds' disabled data-model=\"model\"><img alt='jfkdsljf' />This <!-- > --is-- --> a < test > <broken  test <ns:test ns:attr=\"test\" /></b>";
            var result = string.Join("\n", HtmlLexer.Read(testHtml).Select(x => x.ToString()));

            var expected = "<b class=\"test\" class=\"test2 again\" class=\"kjflds\" disabled data-model=\"model\">" +
                "\n<img alt=\"jfkdsljf\"/>" +
                "\nThis " +
                "\n<!-- > --is-- -->" +
                "\n a " +
                "\n<" +
                "\n test > " +
                "\n<" +
                "\nbroken  test " +
                "\n<ns:test ns:attr=\"test\"/>" +
                "\n</b>";

            Assert.Equal(expected, result);
        }

        [Fact]
        public void CanTrackPosition()
        {
            //              0         1    
            //              01234567890123456789
            var testHtml = "<p>Test</p> <!--a-->";
            var result = HtmlLexer.Read(testHtml).ToList();

            Assert.Equal(0, result[0].Trivia.StartPosition);
            Assert.Equal(3, result[0].Trivia.EndPosition);
            Assert.Equal(3, result[0].Trivia.Length);

            Assert.Equal(3, result[1].Trivia.StartPosition);
            Assert.Equal(7, result[1].Trivia.EndPosition);
            Assert.Equal(4, result[1].Trivia.Length);

            Assert.Equal(7, result[2].Trivia.StartPosition);
            Assert.Equal(11, result[2].Trivia.EndPosition);
            Assert.Equal(4, result[2].Trivia.Length);

            Assert.Equal(11, result[3].Trivia.StartPosition);
            Assert.Equal(12, result[3].Trivia.EndPosition);
            Assert.Equal(1, result[3].Trivia.Length);

            Assert.Equal(12, result[4].Trivia.StartPosition);
            Assert.Equal(20, result[4].Trivia.EndPosition);
            Assert.Equal(8, result[4].Trivia.Length);
        }

        [Fact]
        public void CanParseDoctype()
        {
            "<!DOCTYPE html>".AssertParsesTo(
                Fragment.Doctype(" html")
            );

            "<!doctype html>".AssertParsesTo(
                Fragment.Doctype(" html")
            );
        }
    }
}
