namespace RhythmCastleAP;

internal sealed class HypnoPanReconciler : PreviewAbilityReconcilerBase
{
    internal const string ItemName = "Hypno Pan";
    internal const string AbilityFlag = "PIED_PIPER_ABILITY";

    internal HypnoPanReconciler(IPreviewAbilityNativeAdapter native) : base(native)
    {
    }

    public override string NativeFlag => AbilityFlag;
}
