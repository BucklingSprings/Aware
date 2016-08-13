namespace BucklingSprings.Aware.Collector

open System.Runtime.InteropServices
open BucklingSprings.Aware.Core.Diagnostics
open BucklingSprings.Aware.Core.Models

#nowarn "9"
module NativeHooks =

    type HookProc = delegate of int * System.IntPtr * System.IntPtr -> int

    [<StructLayout(LayoutKind.Sequential)>]
    type PointStruct =
        struct
         val x : int
         val y : int
        end

    type MouseHookStruct =
        struct
            val pt : PointStruct
            val hWnd : System.IntPtr
            val wHitTestCode : System.UIntPtr
            val dwExtraInfo : System.IntPtr
        end


    [<DllImport("user32.dll",CharSet=CharSet.Auto, CallingConvention=CallingConvention.StdCall)>]
    extern int SetWindowsHookEx(int idHook, HookProc lpfn,System.IntPtr hInstance, System.UInt32 threadId)

    [<DllImport("user32.dll",CharSet=CharSet.Auto, CallingConvention=CallingConvention.StdCall)>]
    extern bool UnhookWindowsHookEx(int idHook);

    [<DllImport("user32.dll",CharSet=CharSet.Auto, CallingConvention=CallingConvention.StdCall)>]
    extern int CallNextHookEx(int idHook, int nCode, System.IntPtr wParam, System.IntPtr lParam);


#nowarn

module Hooks =

    let mouseSampleRate = 10
    let mutable kbActivityCount = 0
    let mutable mouseActivityCount = 0
    let mutable mouseActivityCountSample = 0
    let mutable kbHookId = 0
    let mutable mouseHookId = 0

    let kbHookProc nCode wParam lParam =
        System.Threading.Volatile.Write(&kbActivityCount, kbActivityCount + 1)
        NativeHooks.CallNextHookEx(kbHookId, nCode, wParam, lParam)

    let mouseHookProc nCode wParam lParam =
        
        if mouseActivityCount % mouseSampleRate = 0 then
            System.Threading.Volatile.Write(&mouseActivityCountSample, mouseActivityCountSample + 1)    
        mouseActivityCount <- mouseActivityCount + 1
        NativeHooks.CallNextHookEx(mouseHookId, nCode, wParam, lParam)

    let mouseHookDelegate = NativeHooks.HookProc(mouseHookProc)
    let kbHookDelegate = NativeHooks.HookProc(kbHookProc)

    let getAndClearActivity () =
        let rv = {mouseActivity = mouseActivityCountSample; keyboardActivity = kbActivityCount}
        System.Threading.Volatile.Write(&mouseActivityCount, 0)
        System.Threading.Volatile.Write(&mouseActivityCountSample, 0)
        System.Threading.Volatile.Write(&kbActivityCount, 0)
        mouseActivityCount <- 0
        rv


    let setInputHooks () =
        let mouseHook : int = 14
        let kbHook : int = 13
        kbHookId <- NativeHooks.SetWindowsHookEx(kbHook, kbHookDelegate, System.IntPtr.Zero, 0u)
        mouseHookId <- NativeHooks.SetWindowsHookEx(mouseHook, mouseHookDelegate, System.IntPtr.Zero, 0u)
        ()
