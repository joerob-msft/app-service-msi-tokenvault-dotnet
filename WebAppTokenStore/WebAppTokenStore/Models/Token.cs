using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebAppTokenStore.Models
{
    public class Token
    {
        public TokenValueEntity Value { get; set; }

        /// <summary>
        /// Token expiration in seconds, if available.
        /// </summary>
        public long? ExpiresIn { get; set; }
    }

    public class TokenValueEntity
    {
        /// <summary>
        /// Access token.
        /// </summary>
        public string AccessToken { get; set; }
    }
}