namespace Blend.Html.Lexer
{
    public class FragmentTriva
    {
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
        public int Length => EndPosition - StartPosition;
    }
}
