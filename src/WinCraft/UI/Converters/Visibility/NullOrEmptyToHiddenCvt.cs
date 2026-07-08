namespace WinCraft.UI
{
    public sealed class NullOrEmptyToHiddenCvt : ObjectToHiddenCvt
    {
        public NullOrEmptyToHiddenCvt() : base(IsNullOrEmpty) { }
    }
}
