namespace WinCraft.UI
{
    public sealed class NullOrEmptyToVisibleCvt : ObjectToVisibleCvt
    {
        public NullOrEmptyToVisibleCvt() : base(IsNullOrEmpty) { }
    }
}
