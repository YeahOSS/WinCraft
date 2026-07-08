namespace WinCraft.UI
{
    public sealed class NullOrWhiteSpaceToHiddenCvt : ObjectToHiddenCvt
    {
        public NullOrWhiteSpaceToHiddenCvt() : base(IsNullOrWhiteSpace) { }
    }
}
