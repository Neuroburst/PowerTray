using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Wpf.Ui.Input;
using System.IO;
using System.Data;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Xml.Linq;

namespace PowerTray
{
    internal class PowerPlans
    {
        [DllImport("PowrProf.dll")]
        public static extern UInt32 PowerEnumerate(IntPtr RootPowerKey, IntPtr SchemeGuid, IntPtr SubGroupOfPowerSettingGuid, UInt32 AcessFlags, UInt32 Index, ref Guid Buffer, ref UInt32 BufferSize);
        private const uint ACCESS_SCHEME = 16;
        [DllImport("PowrProf.dll")]
        public static extern UInt32 PowerReadFriendlyName(IntPtr RootPowerKey, ref Guid SchemeGuid, IntPtr SubGroupOfPowerSettingGuid, IntPtr PowerSettingGuid, IntPtr Buffer, ref UInt32 BufferSize);

        [DllImportAttribute("powrprof.dll", EntryPoint = "PowerGetActiveScheme")]
        public static extern uint PowerGetActiveScheme(IntPtr UserPowerKey, out IntPtr ActivePolicyGuid);
        [DllImportAttribute("powrprof.dll", EntryPoint = "PowerSetActiveScheme")]
        public static extern uint PowerSetActiveScheme(IntPtr UserPowerKey, ref Guid ActivePolicyGuid);

        public static bool err = false; // for default plan thingy

        public static string ReadFriendlyName(Guid schemeGuid)
        {
            uint sizeName = 1024;
            IntPtr pSizeName = Marshal.AllocHGlobal((int)sizeName);

            string friendlyName;
            try
            {
                PowerReadFriendlyName(IntPtr.Zero, ref schemeGuid, IntPtr.Zero, IntPtr.Zero, pSizeName, ref sizeName);
                friendlyName = Marshal.PtrToStringUni(pSizeName);
            }
            finally
            {
                Marshal.FreeHGlobal(pSizeName);
            }
            return (friendlyName);
        }

        public static void Unlock()
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                CreateNoWindow = false,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Normal,
                FileName = "powershell.exe",
                Arguments = "(gci 'HKLM:\\SYSTEM\\CurrentControlSet\\Control\\Power\\PowerSettings' -Recurse).Name -notmatch '\\bDefaultPowerSchemeValues|(\\\\[0-9]|\\b255)$' | % {sp $_.Replace('HKEY_LOCAL_MACHINE','HKLM:') -Name 'Attributes' -Value 2 -Force}\r\n"
            };

            // Start the process with the info we specified.
            // Call WaitForExit and then the using statement will close.
            using (Process exeProcess = Process.Start(startInfo))
            {
                exeProcess.WaitForExit();
            }
        }

        public static void GeneratePowerPlanList()
        {
            App.plans.Clear();
            foreach (Guid guidPlan in GetPlans())
            {
                PowerPlan plan = new PowerPlan(ReadFriendlyName(guidPlan), guidPlan);
                App.plans.Add(plan);
            }
            App.plans = App.plans.OrderBy(i => i.Name).ToList();
        }

        private static IEnumerable<Guid> GetPlans()
        {
            Guid schemeGuid = Guid.Empty;
            uint sizeSchemeGuid = (uint)Marshal.SizeOf(typeof(Guid));
            uint schemeIndex = 0;

            while (PowerEnumerate(IntPtr.Zero, IntPtr.Zero, IntPtr.Zero,
                ACCESS_SCHEME, schemeIndex,
                ref schemeGuid, ref sizeSchemeGuid) == 0)
            {
                yield return schemeGuid;
                schemeIndex++;
            }
        }

        private static bool FindPlan(Guid guid)
        {
            RefreshPowerPlans();
            foreach (PowerPlan p in App.plans)
            {
                if (p.Guid == guid) { return (true); }
            }
            return (false);
        }
        private static bool FindPlanByName(string name)
        {
            RefreshPowerPlans();
            foreach (PowerPlan p in App.plans)
            {
                if (p.Name == name) { return (true); }
            }
            return (false);
        }

        private static Guid? FindPlanNameToGuid(string name)
        {
            RefreshPowerPlans();
            foreach (PowerPlan p in App.plans)
            {
                if (p.Name == name) { return (p.Guid); }
            }
            return (null);
        }
        private static List<String> AddPowerPlan(string strPowerPlan, string strGuid)
        {
            var messages = new List<String>();
            try
            {
                // Check if we need to add the Plan
                //
                if (FindPlanByName(strPowerPlan) == true)
                {
                    messages.Add($"- '{strPowerPlan}' Power Plan already exists.\n");
                    return messages;
                }

                // Use ProcessStartInfo class
                //
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "powercfg.exe",
                    Arguments = $"-duplicatescheme {strGuid}"
                };

                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                //
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }

                // Check it succeeded
                //
                if (FindPlanByName(strPowerPlan))
                {
                    messages.Add($"- '{strPowerPlan}' Power Plan successfully created.\n");
                }
                else
                {
                    messages.Add($"- Failed creating '{strPowerPlan}' Power Plan.\n");
                    err = true;
                }
            }
            catch (Exception ex)
            {
                // Show exception details.
                messages.Add($"- Exception creating '{strPowerPlan}' Power Plan. [{ex.Message}]\n");
                err = true;
            }

            return (messages);
        }
        private static List<String> ImportPowerPlan(string planpath, string strPowerPlan)
        {
            var messages = new List<String>();
            try
            {
                // Check if we need to add the Plan
                //
                if (FindPlanByName(strPowerPlan) == true)
                {
                    messages.Add($"- '{strPowerPlan}' Power Plan already exists, deleting\n");
                    messages.AddRange(DeletePlan(FindPlanNameToGuid(strPowerPlan)));
                }

                // Use ProcessStartInfo class
                //
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "powercfg.exe",
                    Arguments = $"-import {planpath}"
                };

                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                //
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }

                // Check it succeeded
                //
                if (FindPlanByName(strPowerPlan))
                {
                    messages.Add($"- '{strPowerPlan}' Power Plan successfully created.\n");
                }
                else
                {
                    messages.Add($"- Failed creating '{strPowerPlan}' Power Plan.\n");
                    err = true;
                }
            }
            catch (Exception ex)
            {
                // Show exception details.
                messages.Add($"- Exception creating '{strPowerPlan}' Power Plan. [{ex.Message}]\n");
                err = true;
            }

            return (messages);
        }
        public static List<String> DeletePlan(Guid? guid)
        {
            var messages = new List<String>();
            if (guid != null)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    FileName = "powercfg.exe",
                    Arguments = $"-delete {guid}"
                };
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                }

                if (!FindPlan((Guid)guid))
                {
                    messages.Add($"- Power Plan successfully deleted.\n");
                }
                else
                {
                    messages.Add($"- Failed deleting Power Plan.\n");
                    err = true;
                }
            }
            else
            {
                messages.Add($"- Failed deleting Power Plan.\n");
                err = true;
            }
            return messages;
        }

        public static void ManagePlans(bool boost)
        {
            RefreshPowerPlans();
            //if (IsAdministrator() == false)
            //{
            //    System.Windows.MessageBox.Show(
            //        "This action requires admin access",
            //        "THE CAKE IS A LIE", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            //    return;
            //}
            var messages = new List<String>();
            err = false;
            if (!boost)
            {
                if (App.IsAdministrator())
                {
                    messages.Add("- Applied Registry patches\n");
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power", "PlatformAoAcOverride", 0, RegistryValueKind.DWord);
                    Registry.SetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Power", "CSEnabled", 0, RegistryValueKind.DWord);
                    //Set - ItemProperty - Path 'HKLM:\SYSTEM\CurrentControlSet\Control\Power' - Name 'CSEnabled' - Value 0 - Force
                }
                else
                {
                    messages.Add("- Admin is required to apply Registry patch! (recommended if Power Plans are locked)\n");
                };
                


                messages.AddRange(AddPowerPlan("Power saver",
                                "a1841308-3541-4fab-bc81-f71556f20b4a"));
                messages.AddRange(AddPowerPlan("Balanced",
                                "381b4222-f694-41f0-9685-ff5bb260df2e"));
                messages.AddRange(AddPowerPlan("High performance",
                                "8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"));
                messages.AddRange(AddPowerPlan("Ultimate Performance",
                                "e9a42b02-d5df-448d-aa00-03f14749eb61"));
            }
            else
            {
                var path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\BatteryBoost.pow";
                File.WriteAllBytes(path, PowerTray.Resources.BatteryBoost);
                messages.AddRange(ImportPowerPlan(path, "BatteryBoost"));
                File.Delete(path);
            }


            if (messages.Count > 0 && err)
            {
                string strMessage = String.Concat(messages);

                var popup = new Wpf.Ui.Controls.MessageBox();
                popup.Title = "PowerTray";
                popup.Content = $"Failure.\n{strMessage}";
                //popup.Icon = //MessageBoxImage.Error;// new SymbolIcon(SymbolRegular.Error48, 14, false)
                popup.ShowDialogAsync();

                //System.Windows.MessageBox.Show(
                //    $"Failure.\n{strMessage}",
                //    "PowerTray", System.Windows.MessageBoxButton.OK, MessageBoxImage.Error);
            }
            if (messages.Count > 0 && !err)
            {
                string strMessage = String.Concat(messages);

                var popup = new Wpf.Ui.Controls.MessageBox();
                popup.Title = "PowerTray";
                popup.Content = $"Success!\n{strMessage}";
                //popup.Icon = //MessageBoxImage.Information;// new SymbolIcon(SymbolRegular.Info48, 14, false)
                popup.ShowDialogAsync();

                //System.Windows.MessageBox.Show(
                //    $"Success!\n{strMessage}",
                //    "PowerTray", System.Windows.MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        // End Power App.plans

        public static Guid GetActivePlanGuid()
        {
            Guid ActiveScheme = Guid.Empty;
            if (PowerGetActiveScheme((IntPtr)null, out IntPtr ptr) == 0)
            {
                ActiveScheme = (Guid)Marshal.PtrToStructure(ptr, typeof(Guid));
                Marshal.FreeHGlobal(ptr);
            }
            return (ActiveScheme);
        }
        public static void RefreshPowerPlans()
        {
            GeneratePowerPlanList();
            if (App.pwrPlans != null)
            {
                App.pwrPlans.Items.Clear();
                foreach (PowerPlan plan in App.plans)
                {
                    App.active_plan = GetActivePlanGuid();

                    var item = new Wpf.Ui.Controls.MenuItem()
                    {
                        Header = plan.Name,
                        IsCheckable = true,
                        IsChecked = plan.Guid == App.active_plan,
                        StaysOpenOnClick = true,
                    };
                    item.Command = new RelayCommand<dynamic>(action => SetPlan(plan.Guid, plan.Name), canExecute => true);

                    App.pwrPlans.Items.Add(item);
                }
            }
            if (App.settingsWindow != null)
            {
                App.settingsWindow.UpdatePlansList();
            }

        }

        public static void SetPlan(Guid guid, string name)
        {
            if (FindPlan(guid))
            {
                if (App.notifs && GetActivePlanGuid() != guid)
                {
                    ToastContentBuilder toast = new ToastContentBuilder();
                    toast.AddText("Power Plan Switched");
                    toast.AddText("Switched to " + name + " plan");
                    toast.SetToastDuration(ToastDuration.Short);
                    toast.Show();
                }

                PowerSetActiveScheme(IntPtr.Zero, ref guid);
                RefreshPowerPlans();
                //foreach (Wpf.Ui.Controls.MenuItem menu in pwrPlans.Items)
                //{
                //    menu.IsChecked = false;
                //}
                //item.IsChecked = true;
            }
            else
            {
                if (App.notifs)
                {
                    ToastContentBuilder toast = new ToastContentBuilder();
                    toast.AddText("Failed to Switch Power App.plans");
                    toast.AddText("Tried to Switch to " + name + " plan, but it does not exist");
                    toast.SetToastDuration(ToastDuration.Short);
                    toast.Show();
                }
            }
        }
    }
}
