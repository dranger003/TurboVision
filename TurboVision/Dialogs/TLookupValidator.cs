namespace TurboVision.Dialogs;

/// <summary>
/// Abstract lookup-based validator.
/// Validates input by looking it up in a data source.
/// Derived classes must implement the Lookup method.
/// </summary>
public class TLookupValidator : TValidator
{
    public TLookupValidator() : base()
    {
    }

    public override bool IsValid(string s)
    {
        return Lookup(s);
    }

    /// <summary>
    /// Looks up the input string in the data source.
    /// Override this method in derived classes to provide lookup functionality.
    /// </summary>
    /// <param name="s">The string to look up.</param>
    /// <returns>True if found, false otherwise.</returns>
    public virtual bool Lookup(string s)
    {
        return true;
    }
}
