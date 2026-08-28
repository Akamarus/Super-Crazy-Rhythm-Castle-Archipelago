using System.Reflection;
using System.Reflection.Emit;

namespace RhythmCastleAP;

internal static class RootsComputerNormalization
{
    internal static bool ShouldNormalizeCurrentPhase() =>
        RootsComputerPolicy.ShouldNormalizeState(
            AreaAccessPrototype.Enabled,
            IntroHubSkip.Compatible,
            AreaAccessPrototype.HasArea("Roots"),
            DeveloperHarness.CurrentRoomId);

    internal static DynamicMethod CurrentPhasePostfixFactory(MethodBase original)
    {
        Type phaseType = ((MethodInfo)original).ReturnType;
        var postfix = new DynamicMethod(
            "RootsComputerCurrentPhasePostfix",
            typeof(void),
            new[] { phaseType.MakeByRefType() },
            typeof(RootsComputerNormalization).Module,
            true);
        postfix.DefineParameter(1, ParameterAttributes.None, "__result");
        ILGenerator il = postfix.GetILGenerator();
        Label done = il.DefineLabel();
        il.Emit(OpCodes.Call, typeof(RootsComputerNormalization).GetMethod(
            nameof(ShouldNormalizeCurrentPhase),
            BindingFlags.Static | BindingFlags.NonPublic)!);
        il.Emit(OpCodes.Brfalse_S, done);
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_S, (sbyte)10);
        il.Emit(OpCodes.Stind_I4);
        il.MarkLabel(done);
        il.Emit(OpCodes.Ret);
        return postfix;
    }
}
