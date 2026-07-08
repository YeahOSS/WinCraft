namespace WinCraft.UI
{
    public sealed class FalseToVisibleCvt : ObjectToVisibleCvt
    {
        public FalseToVisibleCvt() : base(IsFalse) { }
    }
}
