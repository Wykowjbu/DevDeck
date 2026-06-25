using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using DevDeck.Contracts;
using System.Diagnostics;

namespace DevDeck.Services
{
    public sealed class ConPtyTerminalBackend : ITerminalBackend
    {
        public event EventHandler<string>? OutputReceived;
        public event EventHandler<int>? Exited;

        private Process? _process;
        private IntPtr _hPC = IntPtr.Zero;
        private SafeFileHandle? _hInputWrite;
        private SafeFileHandle? _hOutputRead;
        private FileStream? _inputStream;
        private FileStream? _outputStream;
        private bool _isDisposed;
        private bool _useFallback;

        // Win32 APIs
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, ref SECURITY_ATTRIBUTES lpPipeAttributes, uint nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetHandleInformation(SafeHandle hObject, int dwMask, int dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int CreatePseudoConsole(COORD size, SafeFileHandle hInput, SafeFileHandle hOutput, uint dwFlags, out IntPtr phPC);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int ResizePseudoConsole(IntPtr hPC, COORD size);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern void ClosePseudoConsole(IntPtr hPC);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CreateProcess(
            string? lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string? lpCurrentDirectory,
            ref STARTUPINFOEX lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UpdateProcThreadAttribute(
            IntPtr lpAttributeList,
            uint dwFlags,
            IntPtr Attribute,
            IntPtr lpValue,
            IntPtr cbSize,
            IntPtr lpPreviousValue,
            IntPtr lpReturnSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeleteProcThreadAttributeList(IntPtr lpAttributeList);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const int HANDLE_FLAG_INHERIT = 0x00000001;
        private const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
        private const IntPtr PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = (IntPtr)0x00020016;

        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            public int nLength;
            public IntPtr lpSecurityDescriptor;
            public bool bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct COORD
        {
            public short X;
            public short Y;

            public COORD(short x, short y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFOEX
        {
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        public async Task StartAsync(
            string shellPath,
            string arguments,
            string workingDirectory,
            int cols,
            int rows,
            CancellationToken cancellationToken)
        {
            try
            {
                // Try initializing ConPTY pseudo console
                InitializeConPty(shellPath, arguments, workingDirectory, cols, rows);
            }
            catch (Exception ex)
            {
                // Fallback to normal Process Standard Redirection
                Debug.WriteLine($"ConPTY initialization failed: {ex.Message}. Falling back to standard redirection.");
                _useFallback = true;
                InitializeProcessRedirection(shellPath, arguments, workingDirectory);
            }

            // Start reading loop
            _ = Task.Run(ReadOutputLoopAsync);
        }

        private void InitializeConPty(string shellPath, string arguments, string workingDirectory, int cols, int rows)
        {
            var sa = new SECURITY_ATTRIBUTES();
            sa.nLength = Marshal.SizeOf(sa);
            sa.bInheritHandle = false;

            // Create input/output pipes
            if (!CreatePipe(out var hInputRead, out _hInputWrite, ref sa, 0))
                throw new InvalidOperationException("Failed to create input pipe");

            if (!CreatePipe(out _hOutputRead, out var hOutputWrite, ref sa, 0))
                throw new InvalidOperationException("Failed to create output pipe");

            // Expose streams
            _inputStream = new FileStream(_hInputWrite, FileAccess.Write, 4096, true);
            _outputStream = new FileStream(_hOutputRead, FileAccess.Read, 4096, true);

            // Create PseudoConsole
            var size = new COORD((short)cols, (short)rows);
            int hr = CreatePseudoConsole(size, hInputRead, hOutputWrite, 0, out _hPC);
            
            // Clean up handles that are now owned by PseudoConsole
            hInputRead.Dispose();
            hOutputWrite.Dispose();

            if (hr != 0)
                throw new InvalidOperationException($"CreatePseudoConsole failed with HRESULT {hr}");

            // Configure process startup info
            var startupInfo = new STARTUPINFOEX();
            startupInfo.StartupInfo.cb = Marshal.SizeOf(startupInfo);

            // Initialize attribute list
            IntPtr lpSize = IntPtr.Zero;
            InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref lpSize);
            startupInfo.lpAttributeList = Marshal.AllocHGlobal(lpSize);
            
            if (!InitializeProcThreadAttributeList(startupInfo.lpAttributeList, 1, 0, ref lpSize))
                throw new InvalidOperationException("InitializeProcThreadAttributeList failed");

            // Update pseudo console attribute
            if (!UpdateProcThreadAttribute(
                startupInfo.lpAttributeList,
                0,
                PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE,
                _hPC,
                (IntPtr)IntPtr.Size,
                IntPtr.Zero,
                IntPtr.Zero))
            {
                throw new InvalidOperationException("UpdateProcThreadAttribute failed");
            }

            string cmdLine = $"\"{shellPath}\" {arguments}".Trim();
            
            var pi = new PROCESS_INFORMATION();
            bool procCreated = CreateProcess(
                null,
                cmdLine,
                IntPtr.Zero,
                IntPtr.Zero,
                false,
                EXTENDED_STARTUPINFO_PRESENT,
                IntPtr.Zero,
                workingDirectory,
                ref startupInfo,
                out pi);

            // Free attribute list
            DeleteProcThreadAttributeList(startupInfo.lpAttributeList);
            Marshal.FreeHGlobal(startupInfo.lpAttributeList);

            if (!procCreated)
                throw new InvalidOperationException($"CreateProcess failed with error {Marshal.GetLastWin32Error()}");

            // Safe handle for process
            var safeProcessHandle = new SafeProcessHandle(pi.hProcess, true);
            CloseHandle(pi.hThread); // We don't need thread handle

            _process = Process.GetProcessById(pi.dwProcessId);
        }

        private void InitializeProcessRedirection(string shellPath, string arguments, string workingDirectory)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = shellPath,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            _process = new Process { StartInfo = startInfo };
            _process.Start();
        }

        private async Task ReadOutputLoopAsync()
        {
            var buffer = new byte[8192];
            try
            {
                if (_useFallback && _process != null)
                {
                    // Fallback read loop
                    var reader = _process.StandardOutput;
                    var errorReader = _process.StandardError;

                    // Read standard output and error in parallel
                    _ = Task.Run(async () =>
                    {
                        var charBuffer = new char[4096];
                        while (!_isDisposed && !_process.HasExited)
                        {
                            int read = await errorReader.ReadAsync(charBuffer, 0, charBuffer.Length);
                            if (read <= 0) break;
                            OutputReceived?.Invoke(this, new string(charBuffer, 0, read));
                        }
                    });

                    var outCharBuffer = new char[4096];
                    while (!_isDisposed && !_process.HasExited)
                    {
                        int read = await reader.ReadAsync(outCharBuffer, 0, outCharBuffer.Length);
                        if (read <= 0) break;
                        OutputReceived?.Invoke(this, new string(outCharBuffer, 0, read));
                    }
                }
                else if (_outputStream != null)
                {
                    // ConPTY read loop
                    while (!_isDisposed)
                    {
                        int read = await _outputStream.ReadAsync(buffer, 0, buffer.Length);
                        if (read <= 0) break;

                        string data = Encoding.UTF8.GetString(buffer, 0, read);
                        OutputReceived?.Invoke(this, data);
                    }
                }
            }
            catch
            {
                // Ignore pipe close exceptions
            }
            finally
            {
                int exitCode = 0;
                try
                {
                    if (_process != null)
                    {
                        await _process.WaitForExitAsync();
                        exitCode = _process.ExitCode;
                    }
                }
                catch { }
                Exited?.Invoke(this, exitCode);
            }
        }

        public async Task WriteAsync(string data, CancellationToken cancellationToken)
        {
            if (_isDisposed) return;

            try
            {
                if (_useFallback && _process != null)
                {
                    await _process.StandardInput.WriteAsync(data);
                    await _process.StandardInput.FlushAsync();
                }
                else if (_inputStream != null)
                {
                    var bytes = Encoding.UTF8.GetBytes(data);
                    await _inputStream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                    await _inputStream.FlushAsync(cancellationToken);
                }
            }
            catch { }
        }

        public Task ResizeAsync(int columns, int rows, CancellationToken cancellationToken)
        {
            if (_isDisposed || _useFallback || _hPC == IntPtr.Zero) return Task.CompletedTask;

            try
            {
                var size = new COORD((short)columns, (short)rows);
                ResizePseudoConsole(_hPC, size);
            }
            catch { }
            return Task.CompletedTask;
        }

        public async Task StopAsync(bool killProcessTree, CancellationToken cancellationToken)
        {
            if (_process != null)
            {
                try
                {
                    if (killProcessTree)
                    {
                        using var killer = Process.Start(new ProcessStartInfo
                        {
                            FileName = "taskkill",
                            Arguments = $"/F /T /PID {_process.Id}",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        });
                        if (killer != null)
                        {
                            await killer.WaitForExitAsync(cancellationToken);
                        }
                    }
                    else
                    {
                        _process.Kill();
                    }
                }
                catch { }
            }

            await DisposeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            if (_isDisposed) return;
            _isDisposed = true;

            try
            {
                _inputStream?.Dispose();
                _outputStream?.Dispose();
                _hInputWrite?.Dispose();
                _hOutputRead?.Dispose();

                if (_hPC != IntPtr.Zero)
                {
                    ClosePseudoConsole(_hPC);
                    _hPC = IntPtr.Zero;
                }

                _process?.Dispose();
            }
            catch { }
        }
    }
}
