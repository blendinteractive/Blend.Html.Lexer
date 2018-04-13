using System.Collections.Generic;
using System.Linq;

namespace Blend.Html.Lexer
{
    public enum DomElementEventType
    {
        /// <summary>
        /// Fragment is child of the current element, but does not "push" onto the stack
        /// </summary>
        Child = 0,

        /// <summary>
        /// Fragment is a child of the current element, and pushes onto the stack
        /// </summary>
        Push,

        /// <summary>
        /// Fragment closes the current element.
        /// </summary>
        Pop
    }

    public class DomElementEvent
    {
        public DomElementEvent(Fragment fragment, DomElementEventType type)
        {
            Fragment = fragment;
            Type = type;
        }

        public Fragment Fragment { get; }
        public DomElementEventType Type { get; }

        public static DomElementEvent Child(Fragment fragment) => new DomElementEvent(fragment, DomElementEventType.Child);
        public static DomElementEvent Push(Fragment fragment) => new DomElementEvent(fragment, DomElementEventType.Push);
        public static DomElementEvent Pop(Fragment fragment) => new DomElementEvent(fragment, DomElementEventType.Pop);
    }

    /// <summary>
    /// Converts a stream of fragments into a DOM, emitting elements
    /// </summary>
    public class LexedDomParser
    {
        // Elements that are always self-closing.
        static readonly HashSet<string> VoidElements = new HashSet<string> { "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr" };

        // Elements that will close an open paragraph tag
        static readonly HashSet<string> ParagraphClosingTags = new HashSet<string> { "address", "article", "aside", "blockquote", "div", "dl", "fieldset", "footer", "form", "h1", "h2", "h3", "h4", "h5", "h6", "header", "hgroup", "hr", "main", "nav", "ol", "p", "pre", "section", "table", "ul" };

        public static bool IsVoidElement(string name) => VoidElements.Contains(name.ToLowerInvariant());
        public static bool IsParagraphClosingTag(string name) => ParagraphClosingTags.Contains(name.ToLowerInvariant());

        readonly Stack<Fragment> stack = new Stack<Fragment>();

        public LexedDomParser() { }

        public static IEnumerable<DomElementEvent> Execute(IEnumerable<Fragment> fragments)
        {
            var parser = new LexedDomParser();
            return parser._Execute(fragments);
        }

        public static IEnumerable<DomElementEvent> Execute(string rawHtml) => Execute(HtmlLexer.Read(rawHtml));

        IEnumerable<DomElementEvent> _Execute(IEnumerable<Fragment> fragments)
        {
            foreach (var fragment in fragments)
            {
                switch (fragment.FragmentType)
                {
                    case FragmentType.Comment:
                    case FragmentType.Doctype:
                    case FragmentType.Text:
                        yield return DomElementEvent.Child(fragment);
                        break;
                    case FragmentType.Close:
                        if (VoidElements.Contains(fragment.Value.ToLowerInvariant()))
                            break;

                        if (stack.Any(x => x.IsNamed(fragment.Value)))
                        {
                            foreach (var close in CloseUntilMatch(fragment.Value, fragment))
                                yield return close;
                        }
                        else
                        {
                            // If there's nothing to close, this is invalid HTML, push through
                            // as a child of the current node, but do not pop the stack.
                            yield return new DomElementEvent(fragment, DomElementEventType.Child);
                        }

                        break;
                    case FragmentType.Open:
                        var nameLower = fragment.Value.ToLowerInvariant();
                        if (ParagraphClosingTags.Contains(nameLower))
                        {
                            foreach (var pClose in CloseUntilMatch("p", null))
                                yield return pClose;
                        }
                        else if (nameLower == "li")
                        {
                            // Find the first UL or LI.
                            var parentLiOrUl = stack.FirstOrDefault(x => x.IsNamed("li") || x.IsNamed("ul"));

                            // If something is found and it's an LI, then we can close it.
                            // (Note: checking for ULs so we don't close an LI from a grand-parent UL)
                            if (parentLiOrUl?.IsNamed("li") == true)
                            {
                                foreach (var liClose in CloseUntilMatch("li", parentLiOrUl.AsCloseFragment()))
                                    yield return liClose;
                            }
                        }

                        bool shouldPush = !fragment.IsSelfClosing && !VoidElements.Contains(fragment.Value.ToLowerInvariant());
                        var eventType = shouldPush ? DomElementEventType.Push : DomElementEventType.Child;
                        yield return new DomElementEvent(fragment, eventType);
                        if (shouldPush)
                            stack.Push(fragment);

                        break;
                }
            }

            while (stack.Count > 0)
            {
                stack.Pop();
                yield return DomElementEvent.Pop(null);
            }
        }

        IEnumerable<DomElementEvent> CloseUntilMatch(string name, Fragment closingFragment)
        {
            // Find an element to close
            var close = stack.FirstOrDefault(x => x.IsNamed(name));

            if (close != null)
            {
                if (stack.Count > 0 && !stack.Peek().IsNamed(name))
                {
                    stack.Pop();
                    yield return DomElementEvent.Pop(null);
                }

                stack.Pop();
                yield return DomElementEvent.Pop(closingFragment);
            }
        }
    }
}
