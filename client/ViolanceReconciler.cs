namespace RhythmCastleAP;

internal sealed class ViolanceReconciler : PreviewAbilityReconcilerBase
{
    internal const string ItemName = "Violance";
    internal const string AbilityFlag = "VIOLIN_ABILITY";

    internal ViolanceReconciler(IPreviewAbilityNativeAdapter native) : base(native)
    {
    }

    public override string NativeFlag => AbilityFlag;
}
