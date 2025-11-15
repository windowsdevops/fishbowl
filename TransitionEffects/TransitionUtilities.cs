// <copyright file="TransitionUtilities.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Transition effects utilities and helper methods.
// </summary>
//-----------------------------------------------------------------------

namespace TransitionEffects
{
    using System;
    using System.Reflection;

    /// <summary>
    /// Static class containing all global helpers.
    /// </summary>
    public static class TransitionUtilities
    {
        #region Fields

        /// <summary>
        /// Short version of assembly name
        /// </summary>
        private static string assemblyShortName;

        #endregion

        #region Properties

        /// <summary>
        /// Gets short version of assembly name, not including entire strong name and version info.
        /// </summary>
        private static string AssemblyShortName
        {
            get
            {
                if (assemblyShortName == null)
                {
                    Assembly a = typeof(TransitionUtilities).Assembly;

                    // Pull out the short name.
                    assemblyShortName = a.ToString().Split(',')[0];
                }

                return assemblyShortName;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Creates Pack Uri given relative file location.
        /// </summary>
        /// <param name="relativeFile">Relative file path.</param>
        /// <returns>Pack Uri corresponding to this part for transition effects.</returns>
        public static Uri MakePackUri(string relativeFile)
        {
            string uriString = "pack://application:,,,/" + AssemblyShortName + ";component/" + relativeFile;
            return new Uri(uriString);
        }

        #endregion
    }
}
