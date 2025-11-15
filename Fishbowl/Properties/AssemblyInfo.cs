
using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Windows;

[assembly: AssemblyTitle("Fishbowl for Facebook")]
[assembly: AssemblyCompany("Microsoft")]
[assembly: AssemblyProduct("Fishbowl for Facebook")]
[assembly: AssemblyCopyright("Copyright Microsoft Corporation.  All rights reserved.")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(false)]
[assembly: NeutralResourcesLanguageAttribute("en-US")]
[assembly: ThemeInfo(ResourceDictionaryLocation.None, ResourceDictionaryLocation.SourceAssembly)]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

// Permission requests
[assembly: SecurityPermission(SecurityAction.RequestMinimum, UnmanagedCode = true, ControlEvidence = true, ControlThread = true, ControlPrincipal = true, RemotingConfiguration = true)]
[assembly: EnvironmentPermission(SecurityAction.RequestMinimum, Unrestricted = true)]
[assembly: FileIOPermission(SecurityAction.RequestMinimum, Unrestricted = true)]
[assembly: RegistryPermission(SecurityAction.RequestMinimum, Unrestricted = true)]
[assembly: ReflectionPermission(SecurityAction.RequestMinimum, Unrestricted = true)]
[assembly: PerformanceCounterPermission(SecurityAction.RequestMinimum, Unrestricted = true)]

