namespace WinCraft.UI
{
    public sealed class NullToHiddenCvt : ObjectToHiddenCvt
    {
        public NullToHiddenCvt() : base(IsNull) { }
    }
}
