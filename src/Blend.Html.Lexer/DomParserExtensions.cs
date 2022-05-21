using System;
using System.Collections.Generic;
using System.Text;

namespace Blend.Html.Lexer
{
    public enum WrapElementsType
    {
        /// <summary>
        /// Adds the wrapping elements outside the matched content
        /// </summary>
        AddOuterWrapper = 0,
        /// <summary>
        /// Adds the wrapping elements within the matched content
        /// </summary>
        AddInnerWrapper,
        /// <summary>
        /// Removes the matched elements, and replaces them with the matched content
        /// </summary>
        ReplaceMatchedElements
    }

    public enum NodeType
    {
        /// <summary>
        /// Process the matching node and inner contents
        /// </summary>
        OuterNode = 0,

        /// <summary>
        /// Ignore the matching node and only process the inner contents
        /// </summary>
        InnerNode
    }

    public class WithinElementDomElementEvent
    {
        public WithinElementDomElementEvent(DomElementEvent elementEvent, bool withinElement)
        {
            ElementEvent = elementEvent;
            WithinElement = withinElement;
        }

        public DomElementEvent ElementEvent { get; }
        public bool WithinElement { get; }
    }

    public static class DomParserExtensions
    {
        /// <summary>
        /// Emits a stream of events
        /// </summary>
        /// <param name="fragments">The lexed HTML fragments</param>
        /// <param name="matchElement">A function to match which element should be considered the "outside" element to process</param>
        /// <param name="includeClosingElementWithin">
        /// If true, IsWithin will be true of the final closing element. This can be handy 
        /// for handling different scenarios. For extracting with the outer value, you probably want this to be true.
        /// For extrating just the inner value, you would want this to be false.
        /// </param>
        /// <returns></returns>
        public static IEnumerable<WithinElementDomElementEvent> WithInElement(this IEnumerable<Fragment> fragments, Func<Fragment, bool> matchElement, bool includeClosingElementWithin)
        {
            bool isInsideFragment = false;
            int stack = 0;

            foreach(var item in LexedDomParser.Execute(fragments))
            {
                switch (item.Type)
                {
                    case DomElementEventType.Push:
                    case DomElementEventType.Child:
                        if (isInsideFragment)
                        {
                            yield return new WithinElementDomElementEvent(item, true);
                            if (item.Type == DomElementEventType.Push)
                            {
                                stack++;
                            }
                        }
                        else
                        {
                            if (matchElement(item.Fragment))
                            {
                                isInsideFragment = true;
                                stack = 1;
                                yield return new WithinElementDomElementEvent(item, true);
                            }
                            else
                            {
                                yield return new WithinElementDomElementEvent(item, false);
                            }
                        }
                        break;
                    case DomElementEventType.Pop:
                        if (isInsideFragment)
                        {
                            stack--;
                            if (stack <= 0)
                                isInsideFragment = false;

                            yield return new WithinElementDomElementEvent(item, includeClosingElementWithin ? true : isInsideFragment);
                        }
                        else
                        {
                            yield return new WithinElementDomElementEvent(item, false);
                        }
                        break;
                }
            }
        }

        public static void ProcessElements(this IEnumerable<WithinElementDomElementEvent> events,
            Action<WithinElementDomElementEvent> outside,
            Action<WithinElementDomElementEvent> onEnter,
            Action<WithinElementDomElementEvent> inside,
            Action<WithinElementDomElementEvent> onExit)
        {
            // Makeshift state machine
            // null = Not yet known if within or without.
            bool? currentlyWithin = null;

            foreach (var ev in events)
            {
                if (!currentlyWithin.HasValue)
                {
                    if (ev.WithinElement)
                    {
                        onEnter?.Invoke(ev);
                        currentlyWithin = true;
                    }
                    else
                    {
                        outside?.Invoke(ev);
                        currentlyWithin = false;
                    }
                }
                else
                {
                    if (currentlyWithin.Value)
                    {
                        if (!ev.WithinElement)
                        {
                            onExit?.Invoke(ev);
                            currentlyWithin = false;
                        }
                        else
                        {
                            inside?.Invoke(ev);
                        }
                    }
                    else
                    {
                        if (ev.WithinElement)
                        {
                            onEnter?.Invoke(ev);
                            currentlyWithin = true;
                        }
                        else
                        {
                            outside?.Invoke(ev);
                        }
                    }
                }
            }
        }

        public static IEnumerable<string> ExtractElementsList(this string html, Func<Fragment, bool> matchElement, NodeType nodeType, bool textOnly = false)
        {
            StringBuilder sb = new StringBuilder();
            List<string> output = new List<string>();

            void appendElement (WithinElementDomElementEvent ev)
            {
                if (ev.ElementEvent.Fragment == null)
                    return;

                if (textOnly && ev.ElementEvent.Fragment.FragmentType != FragmentType.Text)
                    return;

                sb.Append(html, ev.ElementEvent.Fragment.Trivia.StartPosition, ev.ElementEvent.Fragment.Trivia.Length);
            };

            void AddToList()
            {
                output.Add(sb.ToString());
                sb.Clear();
            }

            Action<WithinElementDomElementEvent> doNothing = (ev) => { };

            var events = HtmlLexer.Read(html).WithInElement(matchElement, false);

            events.ProcessElements(
                outside: doNothing,
                onEnter: (ev) =>
                {
                    if (nodeType == NodeType.OuterNode)
                        appendElement(ev);
                },
                inside: appendElement,
                onExit: (ev) =>
                {
                    if (nodeType == NodeType.OuterNode)
                        appendElement(ev);
                    AddToList();
                }
            );

            return output;
        }

        public static string ExtractElements(this string html, Func<Fragment, bool> matchElement, NodeType nodeType)
        {
            var list = ExtractElementsList(html, matchElement, nodeType);
            return string.Join("", list);
        }

        public static IEnumerable<string> ExtractTextList(this string html, Func<Fragment, bool> matchElement)
        {
            return ExtractElementsList(html, matchElement, NodeType.InnerNode, true);
        }


        public static string ExtractText(this string html, Func<Fragment, bool> matchElement)
        {
            var list = ExtractElementsList(html, matchElement, NodeType.InnerNode, true);
            return string.Join("", list);
        }

        public static string ReplaceElements(this string html, Func<Fragment, bool> matchElement, string replacementString, NodeType replaceType)
            => ReplaceElements(html, matchElement, () => replacementString, replaceType);

        public static string ReplaceElements(this string html, Func<Fragment, bool> matchElement, Func<string> replace, NodeType replaceType)
        {
            StringBuilder sb = new StringBuilder(html.Length);

            void AppendElement(WithinElementDomElementEvent ev)
            {
                if (ev.ElementEvent.Fragment != null)
                {
                    sb.Append(html, ev.ElementEvent.Fragment.Trivia.StartPosition, ev.ElementEvent.Fragment.Trivia.Length);
                }
            }

            var events = HtmlLexer.Read(html).WithInElement(matchElement, false);

            bool replaced = false;

            events.ProcessElements(
                outside: AppendElement,
                onEnter: (ev) =>
                {
                    switch (replaceType)
                    {
                        case NodeType.InnerNode:
                            AppendElement(ev);
                            break;
                    }
                },
                inside: (_) =>
                {
                    if (!replaced)
                    {
                        sb.Append(replace?.Invoke());
                        replaced = true;
                    }
                },
                onExit: (ev) =>
                {
                    switch (replaceType)
                    {
                        case NodeType.InnerNode:
                            AppendElement(ev);
                            break;
                    }
                });

            return sb.ToString();
        }

        public static string WrapElements(this string html, Func<Fragment, bool> matchElement, string before, string after, WrapElementsType wrapType)
            => WrapElements(html, matchElement, () => before, () => after, wrapType);

        public static string WrapElements(this string html, Func<Fragment, bool> matchElement, Func<string> before, Func<string> after, WrapElementsType wrapType)
        {
            StringBuilder sb = new StringBuilder(html.Length);

            void AppendElement(WithinElementDomElementEvent ev)
            {
                if (ev.ElementEvent.Fragment != null)
                {
                    sb.Append(html, ev.ElementEvent.Fragment.Trivia.StartPosition, ev.ElementEvent.Fragment.Trivia.Length);
                }
            }

            var events = HtmlLexer.Read(html).WithInElement(matchElement, false);

            events.ProcessElements( 
                outside: AppendElement,
                onEnter: (ev) =>
                {
                    switch (wrapType)
                    {
                        case WrapElementsType.ReplaceMatchedElements:
                            sb.Append(before?.Invoke());
                            break;
                        case WrapElementsType.AddOuterWrapper:
                            sb.Append(before?.Invoke());
                            AppendElement(ev);
                            break;
                        case WrapElementsType.AddInnerWrapper:
                            AppendElement(ev);
                            sb.Append(before?.Invoke());
                            break;
                    }
                },
                inside: AppendElement,
                onExit: (ev) =>
                {
                    switch (wrapType)
                    {
                        case WrapElementsType.ReplaceMatchedElements:
                            sb.Append(after?.Invoke());
                            break;
                        case WrapElementsType.AddOuterWrapper:
                            AppendElement(ev);
                            sb.Append(after?.Invoke());
                            break;
                        case WrapElementsType.AddInnerWrapper:
                            sb.Append(after?.Invoke());
                            AppendElement(ev);
                            break;
                    }
                });

            return sb.ToString();
        }
    }
}
