using System.Collections.Generic;
using System.Linq;
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

        class DomElement
        {
            public List<DomElement> Children { get; } = new List<DomElement>();
            public DomElement Parent { get; }
            public Fragment Fragment { get; }
            public DomElement(DomElement parent, Fragment fragment)
            {
                Parent = parent;
                Fragment = fragment;
            }

            public static DomElement ParseDom(string html)
            {
                var node = new DomElement(null, null);

                foreach (var ev in LexedDomParser.Execute(html))
                {
                    switch (ev.Type)
                    {
                        case DomElementEventType.Push:
                            var child = new DomElement(node, ev.Fragment);
                            node.Children.Add(child);
                            node = child;
                            break;
                        case DomElementEventType.Child:
                            node.Children.Add(new DomElement(node, ev.Fragment));
                            break;
                        case DomElementEventType.Pop:
                            node = node.Parent;
                            break;
                    }
                }

                return node;
            }
        }

        [Fact]
        public void CanBuildDom()
        {
            const string html = "<ul><li>first</li><li>second</li></ul>";
            var node = DomElement.ParseDom(html);

            // Ensure this is the root node.
            Assert.Null(node.Fragment);
            Assert.Null(node.Parent);

            // first child of root is null
            var ul = Assert.Single(node.Children);
            Assert.Equal("ul", ul.Fragment.Value);
            Assert.Equal(2, ul.Children.Count);

            // first li is text node with "first"
            var first = ul.Children[0];
            Assert.Equal("li", first.Fragment.Value);
            Assert.Single(first.Children);
            Assert.Equal("first", first.Children.Single().Fragment.Value);

            // second li is text node with "second"
            var second = ul.Children[1];
            Assert.Equal("li", second.Fragment.Value);
            Assert.Single(second.Children);
            Assert.Equal("second", second.Children.Single().Fragment.Value);
        }
    }
}
