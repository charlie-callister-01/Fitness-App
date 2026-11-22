using System.Diagnostics.CodeAnalysis;

namespace Core.Rules.PlanEditing;

/// <summary>What an edit that can be refused came to: the changed value, or the plain reason it was refused (nothing changes).</summary>
public abstract record PlanEditResult<T>
{
    private PlanEditResult()
    {
    }

    /// <summary>The edit was accepted.</summary>
    public bool IsValid => this is Valid;

    /// <summary>The plain reason the edit was refused, or null when it was accepted.</summary>
    public abstract string? Message { get; }

    /// <summary>True with the changed value when accepted; false with the plain reason when refused.</summary>
    public bool TryGet([MaybeNullWhen(false)] out T value, out string message)
    {
        if (this is Valid valid)
        {
            value = valid.Value;
            message = "";
            return true;
        }

        value = default;
        message = Message ?? "";
        return false;
    }

    /// <summary>Picks one of two results by whether the edit was accepted.</summary>
    public TResult Match<TResult>(Func<T, TResult> valid, Func<string, TResult> invalid) =>
        this is Valid accepted ? valid(accepted.Value) : invalid(Message ?? "");

    public sealed record Valid(T Value) : PlanEditResult<T>
    {
        public override string? Message => null;
    }

    public sealed record Invalid : PlanEditResult<T>
    {
        public Invalid(string message) => Message = message;

        public override string Message { get; }
    }
}
