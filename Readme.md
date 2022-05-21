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

**`WithInElement`** - This helper can be used to process a chunk of HTML by 
matching the parent node.

```csharp
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
```

**`ExtractText`** - This helper extracts text only, leaving out any nodes. It's similar to
`InnerText` in many DOM APIs.


```csharp
    // Include outer node
    const string html = "<body><div id=\"extract\">Extract Me</div></body>";
    var actual = html.ExtractElements(x => x.IsNamed("div") && x.AttributeIs("id", "extract"), NodeType.OuterNode);
    Assert.Equal("<div id=\"extract\">Extract Me</div>", actual);

    // Inner nodes only
    const string html = "<body><div id=\"extract\">Extract Me</div></body>";
    var actual = html.ExtractElements(x => x.IsNamed("div") && x.AttributeIs("id", "extract"), NodeType.InnerNode);
    Assert.Equal("Extract Me", actual);
```

**`ExtractElements`** - This helper can be used to extract a chunk of HTML, either 
the matching node and contents (`NodeType.OuterNode`), or just the inner 
contents (`NodeType.InnerNode`).


```csharp
    // Include outer node
    const string html = "<body><div id=\"extract\">Extract Me</div></body>";
    var actual = html.ExtractElements(x => x.IsNamed("div") && x.AttributeIs("id", "extract"), NodeType.OuterNode);
    Assert.Equal("<div id=\"extract\">Extract Me</div>", actual);

    // Inner nodes only
    const string html = "<body><div id=\"extract\">Extract Me</div></body>";
    var actual = html.ExtractElements(x => x.IsNamed("div") && x.AttributeIs("id", "extract"), NodeType.InnerNode);
    Assert.Equal("Extract Me", actual);
```

**`ExtractElementsList`** - This helper can be used to extract several chunks of HTML, either 
the matching node and contents (`NodeType.OuterNode`), or just the inner 
contents (`NodeType.InnerNode`). Performs much the same function as ``, except this will 
return a list of all elements that matched separately, rather than all the sections together 
in one string.

```csharp
    // Include outer node
    const string html = "<body><section>One</section><section>Two</section></body>";
    var actual = html.ExtractElementsList(x => x.IsNamed("section"), NodeType.OuterNode).ToList();
    Assert.Equal(2, actual.Count);
    Assert.Equal("<section>One</section>", actual[0]);
    Assert.Equal("<section>Two</section>", actual[1]);

    // Inner nodes only
    const string html = "<body><section>One</section><section>Two</section></body>";
    var actual = html.ExtractElementsList(x => x.IsNamed("section"), NodeType.InnerNode).ToList();
    Assert.Equal(2, actual.Count);
    Assert.Equal("One", actual[0]);
    Assert.Equal("Two", actual[1]);
```

**`ReplaceElements`** - This helper is meant to replace an HTML element (and 
all its children) with a string. It can replace the outer node and children, or 
just the children.

```csharp
    // Replace outer node and children
    const string html = "<html><nav id=\"replacement\"><b>Replace <i>me</i></b></nav><footer>Leave me</footer></html>";
    string result = html.ReplaceElements((fragment) => fragment.IsOpen("nav") && fragment.HasAttributeValue("id", "replacement"), "<nav>Replaced</nav>", NodeType.OuterNode);
    Assert.Equal("<html><nav>Replaced</nav><footer>Leave me</footer></html>", result);

    // Replace just the children
    const string html = "<html><body><header>Head!</header><nav class=\"primary\">TO REPLACE</nav></body></html>";
    string result = html.ReplaceElements(x => x.IsOpen("nav"), "<p>Replaced!</p>", NodeType.InnerNode);
    Assert.Equal("<html><body><header>Head!</header><nav class=\"primary\"><p>Replaced!</p></nav></body></html>", result);

```

**`WrapElements`** - This helper can wrap the contents of a matching node, 
either inside the node, outside the node, or replacing the node entirely.

```csharp
    // Wrapping inside the matched node.
    const string html = "<body><div class=\"wrap-me\"><p>This should be wrapped</p></div></body>";
    var updatedContent = html.WrapElements(fragment => fragment.IsNamed("div") && fragment.AttributeIs("class", "wrap-me"), "<span class=\"wrapped\">", "</span>", WrapElementsType.AddInnerWrapper);
    Assert.Equal("<body><div class=\"wrap-me\"><span class=\"wrapped\"><p>This should be wrapped</p></span></div></body>", updatedContent);

    // Wrapping outside the matched node.
    const string html = "<body><div class=\"wrap-me\"><p>This should be wrapped</p></div></body>";
    var updatedContent = html.WrapElements(fragment => fragment.IsNamed("div") && fragment.AttributeIs("class", "wrap-me"), "<span class=\"wrapped\">", "</span>", WrapElementsType.AddOuterWrapper);
    Assert.Equal("<body><span class=\"wrapped\"><div class=\"wrap-me\"><p>This should be wrapped</p></div></span></body>", updatedContent);

    // Replacing the node
    const string html = "<body><div class=\"wrap-me\"><p>This should be wrapped</p></div></body>";
    var updatedContent = html.WrapElements(fragment => fragment.IsNamed("div") && fragment.AttributeIs("class", "wrap-me"), "<span class=\"wrapped\">", "</span>", WrapElementsType.ReplaceMatchedElements);
    Assert.Equal("<body><span class=\"wrapped\"><p>This should be wrapped</p></span></body>", updatedContent);

    // Dynamic node wrapping
    int currentCount = 0;

    const string html = "<ul><li>One</li><li>Two</li></ul>";
    var updatedContent = html.WrapElements(fragment => fragment.IsNamed("li"), 
        () => $"<span id=\"t{currentCount++}\">",
        () => "</span>",
        WrapElementsType.AddInnerWrapper);
    Assert.Equal("<ul><li><span id=\"t0\">One</span></li><li><span id=\"t1\">Two</span></li></ul>", updatedContent);
```

**`ProcessElements`** - A general purpose method for processing elements around a matched node.
This method drives all the other extension methods. You provide actions to at different "events":

* `outside`: Called for every node that is "outside" the matched node
* `onEnter`: Called at the opening of the matched node(s)
* `onExit`: Called at the close of the matched node(s)
* `inside`: Called for every fragment within the matched node

For example:

```csharp
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
        onExit: (ev) => sb.Append($"</b><!-- {count} --></li>"), // Called at the close of the matched node(s)
        inside: (ev) =>
        {
            // Called for every fragment within the matched node
            if (ev.ElementEvent.Fragment != null)
            {
                sb.Append(html, ev.ElementEvent.Fragment.Trivia.StartPosition, ev.ElementEvent.Fragment.Trivia.Length);
            }
        }
    );

    var updatedContent = sb.ToString();
    Assert.Equal("<ul><li id=\"t0\"><b>One</b><!-- 1 --></li><li id=\"t1\"><b>Two</b><!-- 2 --></li></ul>", updatedContent);
```
