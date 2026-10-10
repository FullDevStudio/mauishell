#if MACOS
using CoreFoundation;
using Foundation;
#endif

namespace Shiny.Infrastructure;

public interface IMainThread
{
    Task InvokeOnMainThreadAsync(Action action);
    Task InvokeOnMainThreadAsync(Func<Task> func);
    Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> func);
    void BeginInvokeOnMainThread(Action action);
}

#if MACOS
// MAUI Essentials' MainThread has no AppKit implementation - marshal via NSThread/GCD directly
public class MauiMainThread : IMainThread
{
    public Task InvokeOnMainThreadAsync(Action action)
        => this.InvokeOnMainThreadAsync(() =>
        {
            action.Invoke();
            return Task.FromResult(true);
        });


    public Task InvokeOnMainThreadAsync(Func<Task> func)
        => this.InvokeOnMainThreadAsync(async () =>
        {
            await func.Invoke();
            return true;
        });


    public Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> func)
    {
        if (NSThread.IsMain)
            return func.Invoke();

        var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        BeginInvokeOnMainThread(async () =>
        {
            try
            {
                tcs.TrySetResult(await func.Invoke());
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }


    public void BeginInvokeOnMainThread(Action action)
    {
        if (NSThread.IsMain)
            action();
        else
            DispatchQueue.MainQueue.DispatchAsync(action);
    }
}
#else
public class MauiMainThread : IMainThread
{
    public Task InvokeOnMainThreadAsync(Action action)
    {
        if (OperatingSystem.IsLinux())
        {
            action.Invoke();
            return Task.CompletedTask;
        }
        return MainThread.InvokeOnMainThreadAsync(action);
    }


    public Task InvokeOnMainThreadAsync(Func<Task> func)
    {
        if (OperatingSystem.IsLinux())
            return func.Invoke();

        return MainThread.InvokeOnMainThreadAsync(func);
    }


    public Task<T> InvokeOnMainThreadAsync<T>(Func<Task<T>> func)
    {
        if (OperatingSystem.IsLinux())
            return func.Invoke();

        return MainThread.InvokeOnMainThreadAsync(func);
    }


    public void BeginInvokeOnMainThread(Action action)
    {
        if (OperatingSystem.IsLinux())
            action.Invoke();
        else
            MainThread.BeginInvokeOnMainThread(action);
    }
}
#endif
