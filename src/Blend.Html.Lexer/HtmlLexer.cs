using System;
using System.Collections.Generic;
using PageOfBob.Parsing.Compiled;
using static PageOfBob.Parsing.Compiled.GeneralRules.Rules;
using static PageOfBob.Parsing.Compiled.StringRules.Rules;

namespace Blend.Html.Lexer
{
    public static class HtmlLexer
    {
        static IRule<T> ThenIgnoreWhitespace<T>(this IRule<T> rule, bool required = false) => rule.ThenIgnore(required ? IsWhiteSpaceText.Required() : IsWhiteSpaceText);

        static readonly IRule<char> OpenBracket = Match('<');
        static readonly IRule<char> CloseBracket = Match('>');
        static readonly IRule<char> Slash = Match('/');
        static readonly IRule<char> Equal = Match('=');

        static readonly IRule<string> Name;
        static readonly IRule<string> QuotedValue;
        static readonly IRule<string> UnquotedValue;

        static readonly IRule<Fragment> DoctypeFragment;
        static readonly IRule<Fragment> OpenFragment;
        static readonly IRule<Fragment> CommentFragment;
        static readonly IRule<Fragment> CloseFragment;
        static readonly IRule<Fragment> TextFragment;
        static readonly IRule<Fragment> AllFragments;
        static readonly IRule<List<Fragment>> ScriptFragments;

        static readonly IParser<List<Fragment>> ScriptFragmentsParser;
        static readonly IParser<Fragment> AllFragmentsParser;

        static IRule<string> Quoted(char quoteChar) =>
            Match(quoteChar)
                .ThenKeep(Text(x => x != quoteChar))
                .ThenIgnore(Match(quoteChar));

        static readonly IRule<int> StartPosition = GetPosition;

        static IRule<Fragment> ThenCreateFragment(this IRule<int> rule, FragmentType type) => rule.Map(x =>
        {
            var fragment = new Fragment
            {
                FragmentType = type
            };
            fragment.Trivia.StartPosition = x;
            return fragment;
        });

        static IRule<Fragment> ThenSetEndPosition(this IRule<Fragment> rule) => rule.Then(GetPosition, (node, pos) => { node.Trivia.EndPosition = pos; return node; });
        static IRule<Fragment> ThenSetValue(this IRule<Fragment> rule, IRule<string> value) => rule.Then(value, (node, val) => { node.Value = val; return node; });

        static HtmlLexer()
        {
            Name = IsLetterText.Required()
                .Then(Text(x => char.IsLetter(x) || char.IsDigit(x) || x == '-' || x == '_' || x == ':'), string.Concat);

            // An attribute value with quotes.  "test" / 'test'
            QuotedValue = Any(Quoted('"'), Quoted('\''));

            // An attribute value sans quote
            // The attribute value can remain unquoted if it doesn't contain spaces or any of " ' ` = < or >.
            UnquotedValue =
                Text(x => !char.IsWhiteSpace(x)
                    && x != '<'
                    && x != '>'
                    && x != '\''
                    && x != '"'
                    && x != '='
                    && x != '`'
                );

            // identifer="value"
            // Or identifer=value
            // Or identifier
            var attribute = Name.Map(x => new HtmlAttribute { Key = x })
                .ThenIgnoreWhitespace()
                .Then(
                    Equal
                    .ThenIgnoreWhitespace()
                    .ThenKeep(Any(QuotedValue, UnquotedValue))
                    .Optional(() => null),
                    (attr, val) => { attr.Value = val; return attr; }
                );

            var isSelfClosing = Slash.Map(x => true).Optional(false);

            // <identifier attrs... /?>
            OpenFragment = StartPosition
                .ThenIgnore(OpenBracket)
                .ThenCreateFragment(FragmentType.Open)
                .ThenSetValue(Name).ThenIgnoreWhitespace()
                .Then(attribute.ThenIgnoreWhitespace().Many(), (node, attrs) => { node.Attributes = attrs.ToArray(); return node; })
                .Then(isSelfClosing, (node, sc) => { node.IsSelfClosing = sc; return node; })
                .ThenIgnore(CloseBracket)
                .ThenSetEndPosition();

            // <!-- Comment -->
            var commentEnd = Text("-->");
            CommentFragment = StartPosition
                .ThenIgnore(Text("<!--"))
                .ThenCreateFragment(FragmentType.Comment)
                .ThenSetValue(commentEnd.NotText())
                .ThenIgnore(commentEnd)
                .ThenSetEndPosition();

            // </identifier>
            CloseFragment = StartPosition
                .ThenIgnore(OpenBracket)
                .ThenIgnore(Slash)
                .ThenCreateFragment(FragmentType.Close)
                .ThenSetValue(Name)
                .ThenIgnore(CloseBracket)
                .ThenSetEndPosition();

            // <!DOCTYPE html  ... >
            var doctypeContent = Text(x => x != '>');
            DoctypeFragment = StartPosition
                .ThenIgnore(OpenBracket)
                .ThenIgnore(IText("!doctype"))
                .ThenIgnoreWhitespace(true)
                .ThenCreateFragment(FragmentType.Doctype)
                .ThenSetValue(doctypeContent)
                .ThenIgnore(CloseBracket)
                .ThenSetEndPosition();

            // <script -- requires special text handling, since open/close tags can appear with 
            // reckless abandon within a <script> tag.
            // <script>document.write('<p>Doh!</p>');</script>
            var endScript = CloseFragment.When(x => x.Value != null && string.Compare(x.Value, "script", StringComparison.InvariantCultureIgnoreCase) == 0);
            var scriptContents = StartPosition
                .ThenCreateFragment(FragmentType.Text)
                .ThenSetValue(endScript.NotText())
                .ThenSetEndPosition();
            ScriptFragments = OpenFragment.When(x => string.Compare(x.Value, "script", StringComparison.InvariantCultureIgnoreCase) == 0)
                .Map(x => new List<Fragment>(3) { x })
                .Then(scriptContents, (list, content) => { list.Add(content); return list; })
                .Then(endScript, (list, close) => { list.Add(close); return list; });

            // HTML Tags / Comments
            var htmlFragments = Any(DoctypeFragment, CommentFragment, CloseFragment, OpenFragment);

            // Text is everything else
            TextFragment =
                StartPosition
                .ThenCreateFragment(FragmentType.Text)
                .ThenSetValue(Text(x => x != '<').Required())
                .ThenSetEndPosition();

            // Stray open bracket
            var stray = 
                StartPosition
                .ThenCreateFragment(FragmentType.Text)
                .ThenSetValue(OpenBracket.Map(x => x.ToString()))
                .ThenSetEndPosition();

            // Text and HTML.
            AllFragments = Any(
                htmlFragments,
                TextFragment,
                stray
            );

            ScriptFragmentsParser = ScriptFragments.CompileParser("ScriptFragmentsParser");
            AllFragmentsParser = AllFragments.CompileParser("AllFragmentsParser");
        }

        public static IEnumerable<Fragment> Read(string rawHtml)
        {
            int pos = 0;
            while (pos < rawHtml.Length)
            {
                bool isScript = ScriptFragmentsParser.TryParse(rawHtml, out List<Fragment> result, out int newPos, pos);
                if (isScript)
                {
                    foreach (var item in result)
                        yield return item;
                    pos = newPos;
                }
                else
                {
                    bool isFragment = AllFragmentsParser.TryParse(rawHtml, out Fragment fragment, out int newPos2, pos);
                    if (isFragment)
                    {
                        yield return fragment;
                        pos = newPos2;
                    }
                    else
                    {
                        throw new FormatException();
                    }
                }
            }
        }
    }
}
