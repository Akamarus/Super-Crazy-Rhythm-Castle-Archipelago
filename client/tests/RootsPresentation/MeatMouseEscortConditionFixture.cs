internal class GeneralCondition
{
    public virtual bool CheckIfMet() => false;
}

internal sealed class ExactMouseGeneralCondition : GeneralCondition
{
    private readonly bool _met;
    internal int Calls { get; private set; }

    internal ExactMouseGeneralCondition(bool met) => _met = met;

    public override bool CheckIfMet()
    {
        Calls++;
        return _met;
    }
}

internal sealed class ThrowingMouseGeneralCondition : GeneralCondition
{
    public override bool CheckIfMet() => throw new InvalidOperationException("native read unavailable");
}

internal sealed class WrongMouseCondition
{
    public bool CheckIfMet() => throw new InvalidOperationException("unrelated method must not run");
}

internal sealed class WrongReturnMouseGeneralCondition : GeneralCondition
{
    public new string CheckIfMet() => "not-a-bool";
}
