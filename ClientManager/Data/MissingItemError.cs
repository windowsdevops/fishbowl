//-----------------------------------------------------------------------
// <copyright file="MissingItemError.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <summary>
//     Error data returned when an object is not found.
// </summary>
//-----------------------------------------------------------------------

namespace ClientManager.Data
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Standard;

    /// <summary>
    /// Error data returned when an object is not found, for example if the user tries to view a photo that's no longer in the feed.
    /// </summary>
    public class MissingItemError
    {
        /// <summary>
        /// Initializes a new instance of the MissingItemError class.
        /// </summary>
        /// <param name="message">Message to be displayed for this error.</param>
        public MissingItemError(string message)
        {
            Assert.Fail(message);
            Message = message;
        }

        /// <summary>
        /// Gets the message to display for this error - may contain the path or some information about the missing item.
        /// </summary>
        public string Message { get; private set; }
    }
}
