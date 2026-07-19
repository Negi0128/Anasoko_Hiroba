using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AnasPack
{
    // Windows Vista 以降の「エクスプローラー風」フォルダ選択ダイアログ。
    // 標準の FolderBrowserDialog は古いツリー表示で分かりにくいため、
    // Shell の IFileDialog を FOS_PICKFOLDERS 付きで開いて今風の見た目にする。
    // COM が使えない古い環境では従来の FolderBrowserDialog に自動フォールバックする。
    //
    // ※ Anasoko_Monitor と Anasoko Pack Maker の両方から使うため Shared に置く。
    //   純粋ロジックの Shared/AnasPack（WinForms非依存・テスト対象）とは別フォルダにして、
    //   共有コアのテストに WinForms 依存を持ち込まないようにしている。
    internal static class ModernFolderDialog
    {
        // 選択されたフォルダの絶対パスを返す。キャンセル時は null。
        public static string Show(IWin32Window owner, string title, string initialPath)
        {
            IntPtr ownerHandle = owner?.Handle ?? IntPtr.Zero;
            try
            {
                return ShowVistaDialog(ownerHandle, title, initialPath);
            }
            catch
            {
                // COM 未対応・想定外の環境では従来ダイアログにフォールバックする
                return ShowLegacyDialog(owner, title, initialPath);
            }
        }

        private static string ShowVistaDialog(IntPtr owner, string title, string initialPath)
        {
            var dialog = (IFileDialog)new FileOpenDialogRCW();
            try
            {
                uint options;
                dialog.GetOptions(out options);
                // フォルダを選ばせる（FOS_PICKFOLDERS）＋実在するファイルシステム上の場所に限定（FOS_FORCEFILESYSTEM）
                dialog.SetOptions(options | FOS_PICKFOLDERS | FOS_FORCEFILESYSTEM);

                if (!string.IsNullOrEmpty(title)) dialog.SetTitle(title);

                if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
                {
                    IShellItem startFolder;
                    if (SHCreateItemFromParsingName(initialPath, IntPtr.Zero, typeof(IShellItem).GUID, out startFolder) == 0
                        && startFolder != null)
                    {
                        dialog.SetFolder(startFolder);
                        Marshal.ReleaseComObject(startFolder);
                    }
                }

                // S_OK 以外（キャンセル含む）は選択なしとして扱う
                if (dialog.Show(owner) != 0) return null;

                IShellItem result;
                dialog.GetResult(out result);
                string path;
                result.GetDisplayName(SIGDN_FILESYSPATH, out path);
                Marshal.ReleaseComObject(result);
                return path;
            }
            finally
            {
                Marshal.ReleaseComObject(dialog);
            }
        }

        private static string ShowLegacyDialog(IWin32Window owner, string title, string initialPath)
        {
            using (var dialog = new FolderBrowserDialog { Description = title })
            {
                if (!string.IsNullOrEmpty(initialPath) && Directory.Exists(initialPath))
                {
                    dialog.SelectedPath = initialPath;
                }
                return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.SelectedPath : null;
            }
        }

        private const uint FOS_PICKFOLDERS = 0x00000020;
        private const uint FOS_FORCEFILESYSTEM = 0x00000040;
        private const uint SIGDN_FILESYSPATH = 0x80058000;

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHCreateItemFromParsingName(
            string pszPath, IntPtr pbc, [MarshalAs(UnmanagedType.LPStruct)] Guid riid, out IShellItem ppv);

        // FileOpenDialog コクラス（CLSID）。IFileDialog として使う
        [ComImport, Guid("DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7")]
        private class FileOpenDialogRCW { }

        // IFileDialog（IModalWindow 継承）。呼ぶメソッドだけ正しいシグネチャを与え、
        // 使わないメソッドは vtable の順序を保つためのプレースホルダとして宣言する
        // （COM の vtable スロットは宣言順で決まるため、GetResult まで順番通りに並べる必要がある）。
        [ComImport, Guid("42f85136-db7e-439c-85f1-e4075d135fc8"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IFileDialog
        {
            // --- IModalWindow ---
            [PreserveSig] int Show([In] IntPtr parent);
            // --- IFileDialog ---
            void SetFileTypes();          // 未使用（スロット確保）
            void SetFileTypeIndex();      // 未使用
            void GetFileTypeIndex();      // 未使用
            void Advise();                // 未使用
            void Unadvise();              // 未使用
            void SetOptions(uint fos);
            void GetOptions(out uint fos);
            void SetDefaultFolder();      // 未使用
            void SetFolder([MarshalAs(UnmanagedType.Interface)] IShellItem psi);
            void GetFolder();             // 未使用
            void GetCurrentSelection();   // 未使用
            void SetFileName();           // 未使用
            void GetFileName();           // 未使用
            void SetTitle([MarshalAs(UnmanagedType.LPWStr)] string pszTitle);
            void SetOkButtonLabel();      // 未使用
            void SetFileNameLabel();      // 未使用
            void GetResult([MarshalAs(UnmanagedType.Interface)] out IShellItem ppsi);
            // 以降（AddPlace 等）は使わないので宣言しない
        }

        [ComImport, Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellItem
        {
            void BindToHandler();         // 未使用（スロット確保）
            void GetParent();             // 未使用
            void GetDisplayName(uint sigdnName, [MarshalAs(UnmanagedType.LPWStr)] out string ppszName);
            // 以降（GetAttributes 等）は使わないので宣言しない
        }
    }
}
