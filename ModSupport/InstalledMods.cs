﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TarkovVR.ModSupport
{
    internal static class InstalledMods
    {
        public static bool EFTApiInstalled { get; set; }

        static InstalledMods()
        {
            EFTApiInstalled = false;
        }
    }
}