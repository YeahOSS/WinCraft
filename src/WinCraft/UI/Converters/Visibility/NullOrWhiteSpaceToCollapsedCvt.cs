namespace WinCraft.UI
{
    public sealed class NullOrWhiteSpaceToCollapsedCvt : ObjectToCollapsedCvt
    {
        public NullOrWhiteSpaceToCollapsedCvt() : base(IsNullOrWhiteSpace) { }
    }
}
