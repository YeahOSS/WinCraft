namespace WinCraft.UI
{
    public sealed class NullOrWhiteSpaceToVisibleCvt : ObjectToVisibleCvt
    {
        public NullOrWhiteSpaceToVisibleCvt() : base(IsNullOrWhiteSpace) { }
    }
}
