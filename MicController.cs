using System.Runtime.InteropServices;

namespace Mickey;

/// <summary>
/// 通过 Windows Core Audio API (IAudioEndpointVolume) 控制默认麦克风设备的静音状态。
/// </summary>
public static class MicController
{
    private const uint CLSCTX_ALL = 0x17;
    private const uint STGM_READ = 0;

    private static readonly PropertyKey PKEY_Device_FriendlyName = new()
    {
        FmtId = new Guid("a45c254e-8d1c-4efd-8012-222b6c9d34d2"),
        Pid = 14,
    };

    public static bool GetMuted()
    {
        var volume = GetEndpointVolume();
        volume.GetMute(out bool muted);
        return muted;
    }

    public static void SetMuted(bool mute)
    {
        var volume = GetEndpointVolume();
        volume.SetMute(mute, IntPtr.Zero);
    }

    /// <summary>切换静音状态，返回切换后的状态（true = 已静音）。</summary>
    public static bool Toggle()
    {
        var volume = GetEndpointVolume();
        volume.GetMute(out bool muted);
        volume.SetMute(!muted, IntPtr.Zero);
        return !muted;
    }

    public static string GetDefaultDeviceName()
    {
        try
        {
            var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
            enumerator.GetDefaultAudioEndpoint(EDataFlow.eCapture, ERole.eConsole, out IMMDevice device);
            device.OpenPropertyStore(STGM_READ, out IPropertyStore store);

            var key = PKEY_Device_FriendlyName;
            store.GetValue(ref key, out PropVariant pv);

            string? name = null;
            if (pv.Vt == 31 && pv.Pointer != IntPtr.Zero)
                name = Marshal.PtrToStringUni(pv.Pointer);

            _ = PropVariantClear(ref pv);
            return string.IsNullOrWhiteSpace(name) ? "麦克风" : name.Trim();
        }
        catch
        {
            return "麦克风";
        }
    }

    private static IAudioEndpointVolume GetEndpointVolume()
    {
        var enumerator = (IMMDeviceEnumerator)new MMDeviceEnumeratorComObject();
        enumerator.GetDefaultAudioEndpoint(EDataFlow.eCapture, ERole.eConsole, out IMMDevice device);

        var iid = typeof(IAudioEndpointVolume).GUID;
        device.Activate(ref iid, CLSCTX_ALL, IntPtr.Zero, out object obj);
        return (IAudioEndpointVolume)obj;
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private class MMDeviceEnumeratorComObject
    {
    }

    private enum EDataFlow
    {
        eRender = 0,
        eCapture = 1,
        eAll = 2,
    }

    private enum ERole
    {
        eConsole = 0,
        eMultimedia = 1,
        eCommunications = 2,
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
    }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        void EnumAudioEndpoints(EDataFlow dataFlow, uint dwStateMask, out IMMDeviceCollection ppDevices);
        void GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role, out IMMDevice ppEndpoint);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        void Activate(ref Guid iid, uint dwClsCtx, IntPtr pActivationParams, [MarshalAs(UnmanagedType.IUnknown)] out object ppInterface);
        void OpenPropertyStore(uint stgmAccess, out IPropertyStore ppProperties);
    }

    [ComImport, Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        void GetCount(out uint cProps);
        void GetAt(uint iProp, out PropertyKey pkey);
        void GetValue(ref PropertyKey key, out PropVariant pv);
    }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        void RegisterControlChangeNotify(IntPtr pNotify);
        void UnregisterControlChangeNotify(IntPtr pNotify);
        void GetChannelCount(out uint pnChannelCount);
        void SetMasterVolumeLevel(float fLevelDB, IntPtr pguidEventContext);
        void SetMasterVolumeLevelScalar(float fLevel, IntPtr pguidEventContext);
        void GetMasterVolumeLevel(out float pfLevelDB);
        void GetMasterVolumeLevelScalar(out float pfLevel);
        void SetChannelVolumeLevel(uint nChannel, float fLevelDB, IntPtr pguidEventContext);
        void SetChannelVolumeLevelScalar(uint nChannel, float fLevel, IntPtr pguidEventContext);
        void GetChannelVolumeLevel(uint nChannel, out float pfLevelDB);
        void GetChannelVolumeLevelScalar(uint nChannel, out float pfLevel);
        void SetMute([MarshalAs(UnmanagedType.Bool)] bool bMute, IntPtr pguidEventContext);
        void GetMute([MarshalAs(UnmanagedType.Bool)] out bool pbMute);
        void GetVolumeStepInfo(out uint pnStep, out uint pnStepCount);
        void VolumeStepUp(IntPtr pguidEventContext);
        void VolumeStepDown(IntPtr pguidEventContext);
        void QueryHardwareSupport(out uint pdwHardwareSupportMask);
        void GetVolumeRange(out float pflVolumeMindB, out float pflVolumeMaxdB, out float pflVolumeIncrementdB);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropertyKey
    {
        public Guid FmtId;
        public uint Pid;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PropVariant
    {
        public ushort Vt;
        private ushort _reserved1;
        private ushort _reserved2;
        private ushort _reserved3;
        public IntPtr Pointer;
    }

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant pvar);
}
