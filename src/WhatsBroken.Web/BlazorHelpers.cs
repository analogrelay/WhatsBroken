using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WhatsBroken.Web
{
    static class BlazorHelpers
    {
        public static string DisplayIf(bool visible) => visible ? "display: inherit" : "display: none";
    }
}
