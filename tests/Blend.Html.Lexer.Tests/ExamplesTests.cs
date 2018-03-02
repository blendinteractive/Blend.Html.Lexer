using System.Linq;
using System.Text;
using Xunit;

namespace Blend.Html.Lexer.Tests
{
    public class ExamplesTests
    {
        [Fact]
        public void ExampleCodeWorks()
        {
            const string exampleHtml = @"
<ul>
  <li><a href=""http://www.example.com/"">Example</a></li>
  <li><a href=""http://www.google.com/"">Google</a></li>
  <li><a href=""https://www.yahoo.com/"">Yahoo</a></li>
</ul>";

            const string https = "https://";
            const string http = "http://";

            var fragments = HtmlLexer.Read(exampleHtml);
            var output = new StringBuilder(exampleHtml.Length + 10);

            foreach(var fragment in fragments)
            {
                if (fragment.IsNamed("a") && fragment.HasAttribute("href"))
                {
                    var href = fragment["href"];
                    if (href.Value != null && href.Value.StartsWith(http))
                    {
                        href.Value = https + href.Value.Substring(http.Length);
                    }
                    output.Append(fragment.ToString());
                }
                else
                {
                    output.Append(exampleHtml, fragment.Trivia.StartPosition, fragment.Trivia.Length);
                }
            }

            string actualValue = output.ToString();
            const string expectedValue = @"
<ul>
  <li><a href=""https://www.example.com/"">Example</a></li>
  <li><a href=""https://www.google.com/"">Google</a></li>
  <li><a href=""https://www.yahoo.com/"">Yahoo</a></li>
</ul>";

            Assert.Equal(expectedValue, actualValue);

        }
    }
}
