using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PinToTaskbarHelper.Library
{
    public static class TaskbarPinHelper
    {
        /// <summary>
        /// Thanks goes out to Alex Weinberger (http://alexweinberger.com/main/pinning-network-program-taskbar-programmatically-windows-10/)
        /// for creating a partial solution to this problem.
        /// </summary>
        public static void PinApplication(string executablePath)
        {
            var filesBefore = GetShortcuts();

            string tempDirectoryPath = Path.GetTempPath();
            string tempSubDirectoryName = Guid.NewGuid().ToString();
            string tempSubDirectoryPath = Path.Combine(tempDirectoryPath, tempSubDirectoryName);
            Directory.CreateDirectory(tempSubDirectoryPath);

            string directoryPath = Path.GetDirectoryName(executablePath);
            var directoryInfo = new DirectoryInfo(directoryPath);
            directoryInfo.CopyTo(tempSubDirectoryPath);

            string executableName = Path.GetFileName(executablePath);

            var tempExecutablePath = Path.Combine(tempSubDirectoryPath, executableName);

            try
            {
                ChangeImagePathName("explorer.exe");
                PinUnpinTaskbar(tempExecutablePath, true);
            }
            finally
            {
                RestoreImagePathName();
            }

            var filesAfter = GetShortcuts();

            var newFile = filesAfter.Single(x => !filesBefore.Contains(x));

            ChangeLinkTarget(newFile, executablePath);
            
            Directory.Delete(tempSubDirectoryPath, true);
        }

        private static string[] GetShortcuts()
        {
            var roamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var taskBarPath = Path.Combine(roamingPath, "Microsoft\\Internet Explorer\\Quick Launch\\User Pinned\\TaskBar");

            var files = Directory.EnumerateFiles(taskBarPath);

            return files.ToArray();
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal static extern IntPtr LoadLibrary(string lpLibFileName);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false, ThrowOnUnmappableChar = true)]
        internal static extern int LoadString(IntPtr hInstance, uint wID, StringBuilder lpBuffer, int nBufferMax);

        private static bool PinUnpinTaskbar(string filePath, bool pin)
        {
            if (!File.Exists(filePath)) throw new FileNotFoundException(filePath);
            int MAX_PATH = 255;
            var actionIndex = pin ? 5386 : 5387; // 5386 is the DLL index for"Pin to Tas&kbar", ref. http://www.win7dll.info/shell32_dll.html
                                                 //uncomment the following line to pin to start instead
                                                 //actionIndex = pin ? 51201 : 51394;
            StringBuilder szPinToStartLocalized = new StringBuilder(MAX_PATH);
            IntPtr hShell32 = LoadLibrary("Shell32.dll");
            LoadString(hShell32, (uint)actionIndex, szPinToStartLocalized, MAX_PATH);
            string localizedVerb = szPinToStartLocalized.ToString();

            string path = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileName(filePath);

            // create the shell application object
            dynamic shellApplication = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
            dynamic directory = shellApplication.NameSpace(path);
            dynamic link = directory.ParseName(fileName);

            dynamic verbs = link.Verbs();
            for (int i = 0; i < verbs.Count(); i++)
            {
                dynamic verb = verbs.Item(i);
                if (verb.Name.Equals(localizedVerb))
                {
                    verb.DoIt();
                    return true;
                }
            }
            return false;
        }

        static string originalImagePathName;
        static int unicodeSize = IntPtr.Size * 2;

        private static object Enrivonment { get; set; }

        static void GetPointers(out IntPtr imageOffset, out IntPtr imageBuffer)
        {
            IntPtr pebBaseAddress = GetBasicInformation().PebBaseAddress;
            var processParameters = Marshal.ReadIntPtr(pebBaseAddress, 4 * IntPtr.Size);
            imageOffset = processParameters.Increment(4 * 4 + 5 * IntPtr.Size + unicodeSize + IntPtr.Size + unicodeSize);
            imageBuffer = Marshal.ReadIntPtr(imageOffset, IntPtr.Size);
        }

        internal static void ChangeImagePathName(string newFileName)
        {
            IntPtr imageOffset, imageBuffer;
            GetPointers(out imageOffset, out imageBuffer);

            //Read original data
            var imageLen = Marshal.ReadInt16(imageOffset);
            originalImagePathName = Marshal.PtrToStringUni(imageBuffer, imageLen / 2);

            var newImagePathName = Path.Combine(Path.GetDirectoryName(originalImagePathName), newFileName);
            if (newImagePathName.Length > originalImagePathName.Length) throw new Exception("new ImagePathName cannot be longer than the original one");

            //Write the string, char by char
            var ptr = imageBuffer;
            foreach (var unicodeChar in newImagePathName)
            {
                Marshal.WriteInt16(ptr, unicodeChar);
                ptr = ptr.Increment(2);
            }
            Marshal.WriteInt16(ptr, 0);

            //Write the new length
            Marshal.WriteInt16(imageOffset, (short)(newImagePathName.Length * 2));
        }

        internal static void RestoreImagePathName()
        {
            IntPtr imageOffset, ptr;
            GetPointers(out imageOffset, out ptr);

            foreach (var unicodeChar in originalImagePathName)
            {
                Marshal.WriteInt16(ptr, unicodeChar);
                ptr = ptr.Increment(2);
            }
            Marshal.WriteInt16(ptr, 0);
            Marshal.WriteInt16(imageOffset, (short)(originalImagePathName.Length * 2));
        }

        private static ProcessBasicInformation GetBasicInformation()
        {
            uint status;
            ProcessBasicInformation pbi;
            int retLen;
            var handle = System.Diagnostics.Process.GetCurrentProcess().Handle;
            if ((status = NtQueryInformationProcess(handle, 0,
                out pbi, Marshal.SizeOf(typeof(ProcessBasicInformation)), out retLen)) >= 0xc0000000)
                throw new Exception("Windows exception. status=" + status);
            return pbi;
        }

        [DllImport("ntdll.dll")]
        private static extern uint NtQueryInformationProcess(
            [In] IntPtr ProcessHandle,
            [In] int ProcessInformationClass,
            [Out] out ProcessBasicInformation ProcessInformation,
            [In] int ProcessInformationLength,
            [Out] [Optional] out int ReturnLength
            );

        private static IntPtr Increment(this IntPtr ptr, int value)
        {
            unchecked
            {
                if (IntPtr.Size == sizeof(Int32))
                    return new IntPtr(ptr.ToInt32() + value);
                else
                    return new IntPtr(ptr.ToInt64() + value);
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessBasicInformation
        {
            public uint ExitStatus;
            public IntPtr PebBaseAddress;
            public IntPtr AffinityMask;
            public int BasePriority;
            public IntPtr UniqueProcessId;
            public IntPtr InheritedFromUniqueProcessId;
        }

        private static void ChangeLinkTarget(string shortcutFullPath, string newTarget)
        {
            // Load the shortcut.
            Shell32.Shell shell = new Shell32.Shell();
            Shell32.Folder folder = shell.NameSpace(Path.GetDirectoryName(shortcutFullPath));
            Shell32.FolderItem folderItem = folder.Items().Item(Path.GetFileName(shortcutFullPath));
            Shell32.ShellLinkObject currentLink = (Shell32.ShellLinkObject)folderItem.GetLink;

            // Assign the new path here. This value is not read-only.
            currentLink.Path = newTarget;

            // Save the link to commit the changes.
            currentLink.Save();
        }
    }
}