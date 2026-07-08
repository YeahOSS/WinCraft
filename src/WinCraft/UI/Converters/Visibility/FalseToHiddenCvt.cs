namespace WinCraft.UI
{
    public sealed class FalseToHiddenCvt : ObjectToHiddenCvt
    {
        public FalseToHiddenCvt() : base(IsFalse) { }
    }
}
