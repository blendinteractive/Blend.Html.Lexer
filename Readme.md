Blend HTML Lexer
================

Blend.Html.Lexer is .NET Netstandard library for lexing HTML.  This library is 
intended for when you need to manipulate HTML, without needing a full and 
valid DOM.  It's meant to be reasonably performant, and maintain as much of the 
original (possibly invalid) syntax as possible.

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

Building a DOM
--------------

It is possible to use the lexer to build a valid DOM.  Below is a quick example 
of using the `LexedDomParser` utility to class to build a dom.  The 
`LexedDomParser.Execute` method returns an `IEnumerable` of `DomElementEvent` 
objects representing DOM events.  `Push` events mean a Fragment has been added 
to the stack.  `Child` events mean the fragment is a child of the current 
node, but should not push onto the stack.  `Pop` means an element has been 
closed and the stack should pop.  Note that `Pop` events do not always have an 
associated `Fragment`, in cases where there is no closing element in the HTML.

```csharp
  public class DomElement
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
```

Utilities
---------

There are a few helper utilities included for doing routine manipulations.

**`WithInElement`** - This helper can be used to extract a chunk of HTML by 
matching the parent node.

```csharp
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
```

**`ReplaceElement`** - This helper is meant to replace an HTML element (and 
all its children) with a string.

```csharp
    const string html = "<html><nav id=\"replacement\"><b>Replace <i>me</i></b></nav><footer>Leave me</footer></html>";
    string result = html.ReplaceElement((fragment) => fragment.IsOpen("nav") && fragment.HasAttributeValue("id", "replacement"), "<nav>Replaced</nav>");
    Assert.Equal("<html><nav>Replaced</nav><footer>Leave me</footer></html>", result);
```
