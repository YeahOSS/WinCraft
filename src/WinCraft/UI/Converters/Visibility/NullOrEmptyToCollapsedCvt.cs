namespace WinCraft.UI
{
    public sealed class NullOrEmptyToCollapsedCvt : ObjectToCollapsedCvt
    {
        public NullOrEmptyToCollapsedCvt() : base(IsNullOrEmpty) { }
    }
}
