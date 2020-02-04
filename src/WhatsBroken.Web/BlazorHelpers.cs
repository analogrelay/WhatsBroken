using System;
using System.Collections.Generic;

namespace WhatsBroken.Web
{
    static class BlazorHelpers
    {
        public static IReadOnlyDictionary<string, string> NamespaceAbbreviations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Microsoft", "M" },
            { "AspNetCore", "ANC" },
            { "Extensions", "E" },
            { "FunctionalTests", "FTs" },
            { "Tests", "Ts" },
            { "DotNet", "DN" },
            { "SignalR", "SR" },
            { "Server", "Svr" },
            { "Kestrel", "K" },
            { "InMemory", "InMem" },
            { "EntityFrameworkCore", "EFCore" },
        };

        public static string DisplayIf(bool visible) => visible ? "display: inherit" : "display: none";

        public static string CompactNamespace(string qualifiedName)
        {
            var segments = qualifiedName.Split('.');
            for(var i = 0; i < segments.Length; i += 1)
            {
                if(NamespaceAbbreviations.TryGetValue(segments[i], out var shortForm))
                {
                    segments[i] = shortForm;
                }
            }
            return string.Join('.', segments);
        }
    }
}
