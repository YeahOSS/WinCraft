namespace WinCraft.UI
{
    public sealed class IsNullOrWhiteSpaceCvt : StateConverterBase
    {
        public IsNullOrWhiteSpaceCvt() : base(IsNullOrWhiteSpace, true, false) { }
    }
}
