using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WhatsBroken.Web
{
    static class BlazorHelpers
    {
        public static string DisplayIf(bool visible) => visible ? "display: inherit" : "display: none";

        public static string CompactNamespace(string qualifiedName)
        {
            var segments = qualifiedName.Split('.');
            for(var i = 0; i < segments.Length; i += 1)
            {
                segments[i] = segments[i] switch
                {
                    "Microsoft" => "M",
                    "AspNetCore" => "ANC",
                    "Extensions" => "E",
                    "FunctionalTests" => "FTs",
                    "Tests" => "Ts",
                    "DotNet" => "DN",
                    "SignalR" => "SR",
                    "Server" => "Svr",
                    "Kestrel" => "K",
                    "InMemory" => "InMem",
                    var x => x,
                };
            }
            return string.Join('.', segments);
        }
    }
}
