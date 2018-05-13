using System;
using System.Collections.Generic;
using System.Text;

namespace ASO.Shared
{
    public class ABPResult<TResult>
    {
        /// <summary>
        /// The actual result object of AJAX request.
        /// It is set if <see cref="AjaxResponseBase.Success"/> is true.
        /// </summary>
        public TResult result { get; set; }
        /// <summary>
        /// Indicates success status of the result.
        /// Set <see cref="Error"/> if this value is false.
        /// </summary>
        public bool success { get; set; }

        /// <summary>
        /// Error details (Must and only set if <see cref="Success"/> is false).
        /// </summary>
        public ErrorInfo error { get; set; }

        /// <summary>
        /// This property can be used to indicate that the current user has no privilege to perform this request.
        /// </summary>
        public bool unAuthorizedRequest { get; set; }

        /// <summary>
        /// A special signature for AJAX responses. It's used in the client to detect if this is a response wrapped by ABP.
        /// </summary>
        public bool __abp { get; } = true;
    }

    /// <summary>
    /// Used to store information about an error.
    /// </summary>
    [Serializable]
    public class ErrorInfo
    {
        /// <summary>
        /// Error code.
        /// </summary>
        public int code { get; set; }

        /// <summary>
        /// Error message.
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// Error details.
        /// </summary>
        public string details { get; set; }
        
    }
}
