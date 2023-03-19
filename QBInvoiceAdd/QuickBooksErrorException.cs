using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace oneXerpQB
{
    public class QuickBooksErrorException : Exception
    {
        public int StatusCode { get; }

        public QuickBooksErrorException(int statusCode)
            : base($"QuickBooks error occurred with status code: {statusCode}")
        {
            StatusCode = statusCode;
        }

        public QuickBooksErrorException(int statusCode, string message)
            : base(message)
        {
            StatusCode = statusCode;
        }

        public QuickBooksErrorException(int statusCode, string message, Exception innerException)
            : base(message, innerException)
        {
            StatusCode = statusCode;
        }
    }
}
