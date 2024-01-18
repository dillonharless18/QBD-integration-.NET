using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace erpQB
{
    public class QuickBooksWarningException : Exception
    {
        public int StatusCode { get; }

        public QuickBooksWarningException(int statusCode)
            : base($"QuickBooks warning occurred with status code: {statusCode}")
        {
            StatusCode = statusCode;
        }

        public QuickBooksWarningException(int statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public QuickBooksWarningException(int statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}
