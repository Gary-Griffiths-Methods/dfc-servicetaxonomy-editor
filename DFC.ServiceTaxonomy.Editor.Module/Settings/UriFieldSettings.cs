﻿using System.Collections.Generic;

namespace DFC.ServiceTaxonomy.Editor.Module.Settings
{
    public class UriFieldSettings
    {
        public string? NamespacePrefix { get; set; }
        public List<string> NamespacePrefixOptions { get; set; } = new List<string>();
    }
}
