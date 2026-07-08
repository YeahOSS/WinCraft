namespace WinCraft.UI
{
    public sealed class NullToVisibleCvt : ObjectToVisibleCvt
    {
        public NullToVisibleCvt() : base(IsNull) { }
    }
}
