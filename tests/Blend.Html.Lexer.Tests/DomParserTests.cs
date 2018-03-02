using Xunit;

namespace Blend.Html.Lexer.Tests
{
    public class DomParserTests
    {
        [Fact]
        public void CanRoundtrip()
            => "<p>This is <b>a test</b>!</p>"
                .AssertRoundTripsTo("<p>This is <b>a test</b>!</p>");

        [Fact]
        public void CanHandleBadHtml1()
            => "<p>10 is < 15 and that's true.</p>"
                .AssertRoundTripsTo("<p>10 is < 15 and that's true.</p>"); // Dummy parser doesn't HTML encode entities

        [Fact]
        public void CanHandleBadHtml2()
            => "<p>10 is <15 and that's true.</p>"
                .AssertRoundTripsTo("<p>10 is <15 and that's true.</p>");

        [Fact]
        public void ClosingParagraphTagsIsOptional()
            => "<p>closing<p>is optional"
                .AssertRoundTripsTo("<p>closing</p><p>is optional</p>");

        [Fact]
        public void ClosingParagraphTagsIsOptional2()
            => "<p><b>closing<p>is optional"
                .AssertRoundTripsTo("<p><b>closing</b></p><p>is optional</p>");

        [Fact]
        public void VoidCloseTagsAreIgnored()
            => "<p>bad<br></br>HTML</p>"
                .AssertRoundTripsTo("<p>bad<br>HTML</p>");

        [Fact]
        public void VoidElementAutoClose()
            => "<p>bad<br>HTML</p>"
                .AssertRoundTripsTo("<p>bad<br>HTML</p>"); // Not <p>bad<br>HTML</br></p>

        [Fact]
        public void ClosingLiIsOptions()
            => "<ul><li>test<li>test2</ul><b>test</b>"
                .AssertRoundTripsTo("<ul><li>test</li><li>test2</li></ul><b>test</b>");

        [Fact]
        public void NestedUlLiWork()
            => "<ul><li><ul><li>child</li></ul></li></ul>"
                .AssertRoundTripsTo("<ul><li><ul><li>child</li></ul></li></ul>");
    }
}
