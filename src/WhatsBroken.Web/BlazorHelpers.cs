using Microsoft.AspNetCore.Components;
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

        public static MarkupString GetQueueIcon(string queueName)
        {
            var segments = queueName.Split('.');
            return new MarkupString(segments[0].ToLowerInvariant() switch
            {
                "ubuntu" => "<span class=\"icofont-brand-ubuntu\"></span>",
                "debian" => "<span class=\"icofont-brand-debian\"></span>",
                "raspbian" => "<span class=\"icofont-brand-debian\"></span>",
                "sles" => "<span class=\"icofont-brand-opensuse\"></span>",
                "windows" => "<span class=\"icofont-brand-windows\"></span>",
                "osx" => "<span class=\"icofont-brand-apple\"></span>",
                "redhat" => "<span class=\"icofont-brand-linux\"></span>",
                "centos" => "<span class=\"icofont-brand-linux\"></span>",
                "alpine" => "<span class=\"icofont-brand-linux\"></span>",
                _ => ""
            });
        }
    }
}
