using System;
using System.Runtime.CompilerServices;
using System.Security;
using System.Reflection;
using System.Resources;

// Additional assembly attributes are defined in GlobalAssemblyInfo.cs
[assembly: CLSCompliant(false)]
[assembly: AllowPartiallyTrustedCallers()]
[assembly: SecurityRules(SecurityRuleSet.Level1)]
[assembly: NeutralResourcesLanguage("en", UltimateResourceFallbackLocation.MainAssembly)]
