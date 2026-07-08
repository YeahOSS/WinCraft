namespace WinCraft.UI
{
    public sealed class IsNullOrEmptyCvt : StateConverterBase
    {
        public IsNullOrEmptyCvt() : base(IsNullOrEmpty, true, false) { }
    }
}
