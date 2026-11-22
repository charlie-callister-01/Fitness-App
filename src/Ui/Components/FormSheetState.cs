namespace Ui.Components;

/// <summary>What a <see cref="FormSheet"/> tells its content about the save: whether one is running and whether this window can write.</summary>
public readonly record struct FormSheetState(bool Saving, bool CanWrite)
{
    /// <summary>The rule every sheet's write buttons follow: wait while saving, and wait for good in a read-only window.</summary>
    public bool WriteDisabled => Saving || !CanWrite;
}
