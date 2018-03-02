Blend HTML Lexer
================

Blend.Html.Lexer is .NET Netstandard library for lexing HTML.  This library is 
intended for when you need to manipulation HTML, without needing a full and 
valid DOM.

For example, to replace all links starting with `http://` with `https://`:

```csharp

const string exampleHtml = @"
<ul>
  <li><a href=""http://www.example.com/"">Example</a></li>
  <li><a href=""http://www.google.com/"">Google</a></li>
  <li><a href=""https://www.yahoo.com/"">Yahoo</a></li>
</ul>";

const string https = "https://";
const string http = "http://";

// Will lex the HTML into fragments
var fragments = HtmlLexer.Read(exampleHtml);

// StringBuilder to store new output
var output = new StringBuilder(exampleHtml.Length + 10);

foreach(var fragment in fragments)
{
	// If the element is an A tag and has an HREF attribute
    if (fragment.IsNamed("a") && fragment.HasAttribute("href"))
    {
        var href = fragment["href"];
        // Replace http with https
        if (href.Value != null && href.Value.StartsWith(http))
        {
            href.Value = https + href.Value.Substring(http.Length);
        }

        // NOTE: fragment.ToString() does not HTML encode.  Fragments assume their content
        // is already HTML encoded.
        output.Append(fragment.ToString());
    }
    else
    {
        // Otherwise, output the fragment as-is from the original HTML without allocating a string.
        output.Append(exampleHtml, fragment.Trivia.StartPosition, fragment.Trivia.Length);
    }
}

// Validate result
string actualValue = output.ToString();
const string expectedValue = @"
<ul>
  <li><a href=""https://www.example.com/"">Example</a></li>
  <li><a href=""https://www.google.com/"">Google</a></li>
  <li><a href=""https://www.yahoo.com/"">Yahoo</a></li>
</ul>";

Assert.Equal(expectedValue, actualValue);
```

