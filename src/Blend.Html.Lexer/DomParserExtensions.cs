using System;
using System.Collections.Generic;
using System.Text;

namespace Blend.Html.Lexer
{
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
        public static IEnumerable<WithinElementDomElementEvent> WithInElement(this IEnumerable<Fragment> fragments, Func<Fragment, bool> matchElement)
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
                            yield return new WithinElementDomElementEvent(item, true);

                            stack--;
                            if (stack <= 0)
                                isInsideFragment = false;
                        }
                        else
                        {
                            yield return new WithinElementDomElementEvent(item, false);
                        }
                        break;
                }
            }
        }

        public static string ReplaceElement(this string html, Func<Fragment, bool> matchElement, string replacementText)
        {
            StringBuilder sb = new StringBuilder();
            bool replaced = false;

            var events = HtmlLexer.Read(html).WithInElement(matchElement);

            foreach(var ev in events)
            {
                if (ev.WithinElement)
                {
                    if (!replaced)
                        sb.Append(replacementText);
                    replaced = true;
                }
                else
                {
                    if (ev.ElementEvent.Fragment != null)
                    {
                        sb.Append(html, ev.ElementEvent.Fragment.Trivia.StartPosition, ev.ElementEvent.Fragment.Trivia.Length);
                    }
                }
            }

            return sb.ToString();
        }
    }
}
