namespace WinCraft.UI
{
    public sealed class NullToCollapsedCvt : ObjectToCollapsedCvt
    {
        public NullToCollapsedCvt() : base(IsNull) { }
    }
}
