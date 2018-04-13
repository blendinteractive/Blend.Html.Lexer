using Xunit;

namespace Blend.Html.Lexer.Tests
{
    public class FragmentFacts
    {
        static readonly Fragment TestFragment = Fragment.OpenTag("a", true,
            HtmlAttribute.Create("name", "target"),
            HtmlAttribute.Create("class", "inactive"),
            HtmlAttribute.Create("disabled"),
            HtmlAttribute.Create("class", "override")
        );

        [Fact]
        public void CanLookupAttributeValue() => Assert.Equal("target", TestFragment.GetAttributeValue("name", null));

        [Fact]
        public void NonExistantAttributeReturnsDefaultValue() => Assert.Equal("#", TestFragment.GetAttributeValue("href", "#"));

        [Fact]
        public void CanLookupHtmlAttributeObject() => Assert.Equal(HtmlAttribute.Create("name", "target"), TestFragment["name"]);

        [Fact]
        public void NonExistantReturnsNull() => Assert.Null(TestFragment["href"]);

        [Fact]
        public void ExistsReturnsTrueIfExists() => Assert.True(TestFragment.HasAttribute("disabled"));

        [Fact]
        public void ExistsReturnsFalseIfNotExists() => Assert.False(TestFragment.HasAttribute("data-nothing"));

        [Fact]
        public void NoValueAttributesReturnNullNotDefaultValue() => Assert.Null(TestFragment.GetAttributeValue("disabled", "false"));

        [Fact]
        public void LastDuplicateAttributeWins() => Assert.Equal("override", TestFragment.GetAttributeValue("class"));

        [Fact]
        public void IsClosedReturnsCloseTags()
        {
            var t = Fragment.CloseTag("a");
            Assert.True(t.IsClose("a"));
            Assert.False(t.IsClose("b"));
            Assert.True(t.IsClose());
        }

        [Fact]
        public void IsClosedFalseForSelfClosing()
        {
            var t2 = Fragment.OpenTag("a", true);
            Assert.False(t2.IsClose("a"));
        }
    }
}
