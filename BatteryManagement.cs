using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Power;
using System.Collections;
using System.Collections.Specialized;

namespace PowerTray
{
    internal class BatteryManagement
    {
        public static OrderedDictionary GetBatteryInfo(uint batteryTag, SafeFileHandle batteryHandle) // MOVE TO BATTERY MANAGEMENTTNTIT
        {
            var batteryReport = Battery.AggregateBattery.GetReport(); // get battery info (slow)

            //var batteries = new ManagementObjectSearcher("SELECT * FROM CIM_Battery").Get(); //get advanced battery info
            //ManagementObject main_battery = new ManagementObject();
            ////gets the first battery using the dumbest way possible :) USE INDEX INSTEAD
            //foreach (ManagementObject battery in batteries)
            //{
            //    Debug.Print(battery.ToString());
            //    main_battery = battery;
            //    break;
            //};

            var dataDict = new OrderedDictionary { };
            
            Kernel32.BATTERY_WAIT_STATUS bws = default;
            bws.BatteryTag = batteryTag;
            Kernel32.BATTERY_STATUS batteryStatus = default;

            int remainChargeCapMwh = 0;
            int chargeRate = 0;
            int voltage = 0;
            if (Kernel32.DeviceIoControl(batteryHandle,
                                         Kernel32.IOCTL.IOCTL_BATTERY_QUERY_STATUS,
                                         ref bws,
                                         Marshal.SizeOf(bws),
                                         ref batteryStatus,
                                         Marshal.SizeOf(batteryStatus),
                                         out _,
                                         IntPtr.Zero))
            {
                remainChargeCapMwh = (int)batteryStatus.Capacity;
                chargeRate = (int)batteryStatus.Rate;
                voltage = (int)batteryStatus.Voltage / 1000;
            }
            dataDict.Add("Status", batteryReport.Status);
            dataDict.Add("Percent Remaining", (remainChargeCapMwh / (double)batteryReport.FullChargeCapacityInMilliwattHours) * 100);
            dataDict.Add("Remaining Charge mWh", remainChargeCapMwh);
            dataDict.Add("Charge Rate mW", chargeRate);

            dataDict.Add("Battery Health", (double)batteryReport.FullChargeCapacityInMilliwattHours / (double)batteryReport.DesignCapacityInMilliwattHours);
            dataDict.Add("Battery Capacity mWh", batteryReport.FullChargeCapacityInMilliwattHours);
            dataDict.Add("Design Capacity mWh", batteryReport.DesignCapacityInMilliwattHours);
            dataDict.Add("Voltage", voltage);

            //dataDict.Add("----", "----");
            //foreach (PropertyData property in main_battery.Properties)
            //{
            //    if (property.Value != null)
            //    {
            //        dataDict.Add(property.Name, property.Value);
            //    }
            //}

            return dataDict;
        }
        public static unsafe dynamic[] GetBatteryTag()
        {
            uint batteryTag = 0;
            SafeFileHandle batteryHandle = null;

            IntPtr hdev = SetupApi.SetupDiGetClassDevs(ref SetupApi.GUID_DEVICE_BATTERY, IntPtr.Zero, IntPtr.Zero, SetupApi.DIGCF_PRESENT | SetupApi.DIGCF_DEVICEINTERFACE);
            if (hdev != SetupApi.INVALID_HANDLE_VALUE)
            {
                for (uint i = 0; ; i++)
                {
                    SetupApi.SP_DEVICE_INTERFACE_DATA did = default;
                    did.cbSize = (uint)Marshal.SizeOf(typeof(SetupApi.SP_DEVICE_INTERFACE_DATA));

                    if (!SetupApi.SetupDiEnumDeviceInterfaces(hdev,
                                                              IntPtr.Zero,
                                                              ref SetupApi.GUID_DEVICE_BATTERY,
                                                              i,
                                                              ref did))
                    {
                        if (Marshal.GetLastWin32Error() == SetupApi.ERROR_NO_MORE_ITEMS)
                            break;
                    }
                    else
                    {
                        SetupApi.SetupDiGetDeviceInterfaceDetail(hdev,
                                                                 did,
                                                                 IntPtr.Zero,
                                                                 0,
                                                                 out uint cbRequired,
                                                                 IntPtr.Zero);

                        if (Marshal.GetLastWin32Error() == SetupApi.ERROR_INSUFFICIENT_BUFFER)
                        {
                            IntPtr pdidd = Kernel32.LocalAlloc(Kernel32.LPTR, cbRequired);
                            Marshal.WriteInt32(pdidd, Environment.Is64BitProcess ? 8 : 4 + Marshal.SystemDefaultCharSize); // cbSize.

                            if (SetupApi.SetupDiGetDeviceInterfaceDetail(hdev,
                                                                         did,
                                                                         pdidd,
                                                                         cbRequired,
                                                                         out _,
                                                                         IntPtr.Zero))
                            {
                                string devicePath = new((char*)(pdidd + 4));
                                SafeFileHandle battery = Kernel32.CreateFile(devicePath, FileAccess.ReadWrite, FileShare.ReadWrite, IntPtr.Zero, FileMode.Open, FileAttributes.Normal, IntPtr.Zero);
                                batteryHandle = battery;
                                if (!battery.IsInvalid)
                                {
                                    Kernel32.BATTERY_QUERY_INFORMATION bqi = default;

                                    uint dwWait = 0;
                                    if (Kernel32.DeviceIoControl(battery,
                                                                 Kernel32.IOCTL.IOCTL_BATTERY_QUERY_TAG,
                                                                 ref dwWait,
                                                                 Marshal.SizeOf(dwWait),
                                                                 ref bqi.BatteryTag,
                                                                 Marshal.SizeOf(bqi.BatteryTag),
                                                                 out _,
                                                                 IntPtr.Zero))
                                    {
                                        batteryTag = bqi.BatteryTag;
                                    }
                                }
                            }
                            Kernel32.LocalFree(pdidd);
                        }
                    }
                }
                SetupApi.SetupDiDestroyDeviceInfoList(hdev);
            }
            return [batteryHandle, batteryTag];
        }
    }
    internal class SetupApi
    {
        internal const int DIGCF_DEVICEINTERFACE = 0x00000010;
        internal const int DIGCF_PRESENT = 0x00000002;
        internal const int ERROR_INSUFFICIENT_BUFFER = 122;
        internal const int ERROR_NO_MORE_ITEMS = 259;

        private const string DllName = "SetupAPI.dll";
        internal static Guid GUID_DEVICE_BATTERY = new(0x72631e54, 0x78A4, 0x11d0, 0xbc, 0xf7, 0x00, 0xaa, 0x00, 0xb7, 0xb3, 0x2a);
        internal static readonly IntPtr INVALID_HANDLE_VALUE = new(-1);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        internal static extern IntPtr SetupDiGetClassDevs(ref Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, uint Flags);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetupDiEnumDeviceInterfaces
            (IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref Guid InterfaceClassGuid, uint MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport(DllName, SetLastError = true, EntryPoint = "SetupDiGetDeviceInterfaceDetailW", CharSet = CharSet.Unicode)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetupDiGetDeviceInterfaceDetail
        (
            IntPtr DeviceInfoSet,
            in SP_DEVICE_INTERFACE_DATA DeviceInterfaceData,
            [Out, Optional] IntPtr DeviceInterfaceDetailData,
            uint DeviceInterfaceDetailDataSize,
            out uint RequiredSize,
            IntPtr DeviceInfoData = default);

        [DllImport(DllName, CallingConvention = CallingConvention.Winapi, SetLastError = true)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [StructLayout(LayoutKind.Sequential)]
        internal struct SP_DEVICE_INTERFACE_DATA
        {
            public uint cbSize;
            public Guid InterfaceClassGuid;
            public uint Flags;
            public IntPtr Reserved;
        }
    }
}
