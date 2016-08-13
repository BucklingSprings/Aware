namespace BucklingSprings.Aware.Core

open System
open System.Runtime.InteropServices

module Win32 =

   [<DllImport("kernel32.dll", SetLastError=true, CharSet=CharSet.Auto)>]
   extern [<MarshalAs(UnmanagedType.Bool)>] bool GetDiskFreeSpaceEx(string lpDirectoryName,UInt64& lpFreeBytesAvailable,UInt64& lpTotalNumberOfBytes,UInt64& lpTotalNumberOfFreeBytes)

