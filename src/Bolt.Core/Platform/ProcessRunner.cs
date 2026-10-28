using System.Diagnostics;
using System.Text;
using Bolt.Core.Resources;

namespace Bolt.Core.Platform;

public sealed record ProcessResult(int ExitCode, string StandardOutput, string StandardError)
{
    public bool Succeeded => ExitCode == 0;

    /// <summary>The last lines of stderr (or stdout), for error messages.</summary>
    public string Tail
    {
        get
        {
            var text = string.IsNullOrWhiteSpace(StandardError) ? StandardOutput : StandardError;
            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            return string.Join('\n', lines.TakeLast(5));
        }
    }
}

public class InstallerException(string message, Exception? inner = null) : Exception(message, inner);

public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(string fileName, IEnumerable<string> arguments, CancellationToken ct = default);
}

public sealed class ProcessRunner : IProcessRunner
{
    public async Task<ProcessResult> RunAsync(string fileName, IEnumerable<string> arguments,
                                              CancellationToken ct = default)
    {
        var info = new ProcessStartInfo(fileName)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
            info.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = info };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) stdout.AppendLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) stderr.AppendLine(e.Data); };

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            throw new InstallerException(string.Format(Strings.CannotStartReason, fileName, ex.Message), ex);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(ct).ConfigureAwait(false);
        return new ProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }
}
