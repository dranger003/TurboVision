namespace TurboVision.Views;

/// <summary>
/// Base class for TurboVision objects.
/// Provides virtual destruction pattern.
/// </summary>
public abstract class TObject : IDisposable
{
    private bool _disposed;

    ~TObject()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                ShutDown();
            }
            _disposed = true;
        }
    }

    /// <summary>
    /// Called during shutdown to clean up resources.
    /// Override this instead of Dispose for TurboVision-style cleanup.
    /// </summary>
    public virtual void ShutDown()
    {
    }
}
