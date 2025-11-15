//-----------------------------------------------------------------------
// <copyright file="Global.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Helper methods
// </summary>
//-----------------------------------------------------------------------
namespace EffectLibrary
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Helper global class for extensible effects.
    /// </summary>
    internal static class Global
    {
        /// <summary>
        /// The short name of the this assembly.
        /// </summary>
        private static string assemblyShortName;

        /// <summary>
        /// Gets the short name of the this assembly without the file extension.
        /// </summary>
        private static string AssemblyShortName
        {
            get
            {
                if (assemblyShortName == null)
                {
                    Assembly a = typeof(Global).Assembly;

                    // Pull out the short name.
                    assemblyShortName = a.ToString().Split(',')[0];
                }

                return assemblyShortName;
            }
        }

        /// <summary>
        /// Creates a pack URI to an embedded file within the EffectLibrary assembly.
        /// </summary>
        /// <param name="relativeFile">The name of the Embedded Resource file.</param>
        /// <returns>A pack URI.</returns>
        public static Uri MakePackUri(string relativeFile)
        {
            string uriString = "pack://application:,,,/" + AssemblyShortName + ";component/" + relativeFile;
            return new Uri(uriString);
        }
    }
}
