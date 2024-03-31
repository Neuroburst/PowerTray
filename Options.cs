using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerTray
{
    internal class Options:ConfigurationSection
    {
        [ConfigurationProperty("alwaysontop", DefaultValue = true)]
        public bool AlwaysOnTop
        {
            get { return (bool)this["alwaysontop"]; }
            set { this["alwaysontop"] = value; }
        }

        [ConfigurationProperty("fontSize", DefaultValue = 11f)]
        public float FontSize
        {
            get { return (float)this["fontSize"]; }
            set { this["fontSize"] = value; }
        }

        [ConfigurationProperty("buffersize", DefaultValue = 60000)]
        public int BufferSize
        {
            get { return (int)this["buffersize"]; }
            set { this["buffersize"] = value; }
        }

        [ConfigurationProperty("trayrefreshrate", DefaultValue = 1000)]
        public int TrayRefreshRate
        {
            get { return (int)this["trayrefreshrate"]; }
            set { this["trayrefreshrate"] = value; }
        }

        [ConfigurationProperty("batrefreshrate", DefaultValue = 500)]
        public int BatInfoRefreshRate
        {
            get { return (int)this["batrefreshrate"]; }
            set { this["batrefreshrate"] = value; }
        }

        [ConfigurationProperty("mediumbatcharge", DefaultValue = 40)]
        public int MediumCharge
        {
            get { return (int)this["mediumbatcharge"]; }
            set { this["mediumbatcharge"] = value; }
        }

        [ConfigurationProperty("lowbatcharge", DefaultValue = 25)]
        public int LowCharge
        {
            get { return (int)this["lowbatcharge"]; }
            set { this["lowbatcharge"] = value; }
        }

        [ConfigurationProperty("traytext", DefaultValue = App.DisplayedInfo.percentage)]
        public App.DisplayedInfo TrayText
        {
            get { return (App.DisplayedInfo)this["traytext"]; }
            set { this["traytext"] = value; }
        }

        [ConfigurationProperty("fontstyle", DefaultValue = FontStyle.Bold)]
        public FontStyle FontStyle
        {
            get { return (FontStyle)this["fontstyle"]; }
            set { this["fontstyle"] = value; }
        }
    }
}
