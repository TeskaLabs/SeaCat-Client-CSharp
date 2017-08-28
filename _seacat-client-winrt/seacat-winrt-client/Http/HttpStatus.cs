using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace seacat_winrt_client.Http {

    public static class HttpStatus {
        private static Dictionary<int, string> messages = new Dictionary<int, string>()
        {
            // 1xx codes
            {(int) HttpStatusCode.Continue, "Continue"}, // 100
            {(int) HttpStatusCode.SwitchingProtocols, "Switching Protocols"}, // 101
            {102, "Processing"},

            // 2xx codes
            {(int)HttpStatusCode.OK, "OK"}, // 200
            {(int)HttpStatusCode.Created, "Created"}, // 201
            {(int)HttpStatusCode.Accepted, "Accepted"}, // 202
            {(int)HttpStatusCode.NonAuthoritativeInformation, "Not Authoritative Information"}, // 203
            {(int)HttpStatusCode.NoContent, "No Content"}, // 204
            {(int)HttpStatusCode.ResetContent, "Reset Content"}, // 205
            {(int)HttpStatusCode.PartialContent, "Partial Content"}, // 206
            {207, "Multi-Status"},

            // 3xx codes
            {(int)HttpStatusCode.MultipleChoices, "Multiple Choices"}, // 300
            {(int)HttpStatusCode.MovedPermanently, "Moved Permanently"}, // 301
            {302, "Moved Temporarily"}, // 302
            {(int)HttpStatusCode.SeeOther, "See Other"}, // 303
            {(int)HttpStatusCode.NotModified, "Not Modified"}, // 304
            {(int)HttpStatusCode.UseProxy, "Use Proxy"}, // 305
            {(int)HttpStatusCode.TemporaryRedirect, "Temporary Redirect"}, // 307

            // 4xx codes
            {(int)HttpStatusCode.BadRequest, "Bad Request"}, // 400
            {(int)HttpStatusCode.Unauthorized, "Unauthorized"}, // 401
            {(int)HttpStatusCode.PaymentRequired, "Payment Required"}, // 402
            {(int)HttpStatusCode.Forbidden, "Forbidden"}, // 403
            {(int)HttpStatusCode.NotFound, "Not Found"}, // 404
            {(int)HttpStatusCode.MethodNotAllowed, "Method Not Allowed"}, // 405
            {(int)HttpStatusCode.NotAcceptable, "Not Acceptable"}, // 406
            {(int)HttpStatusCode.ProxyAuthenticationRequired, "Proxy Authentication Required"}, // 407
            {(int)HttpStatusCode.RequestTimeout, "Request Timeout"}, // 408
            {(int)HttpStatusCode.Conflict, "Conflict"}, // 409
            {(int)HttpStatusCode.Gone, "Gone"}, // 410
            {(int)HttpStatusCode.LengthRequired, "Length Required"}, // 411
            {(int)HttpStatusCode.PreconditionFailed, "Precondition Failed"}, // 412
            {(int)HttpStatusCode.RequestEntityTooLarge, "Request Too Long"}, // 413
            {(int)HttpStatusCode.RequestUriTooLong, "Request-URI Too Long"}, // 414
            {(int)HttpStatusCode.UnsupportedMediaType, "Unsupported Media Type"}, // 415
            {(int)HttpStatusCode.RequestedRangeNotSatisfiable, "Requested Range Not Satisfable"}, // 416
            {(int)HttpStatusCode.ExpectationFailed, "Expectation Failed"}, // 417
            {418, "Unprocessable Entity"}, // 418
            {419, "Insufficient Space On Resource"}, // 419
            {420, "Method Failure"}, // 420

            {423, "Locked"},
            {424, "Failed Dependency"},

            // 5xx codes
            {(int)HttpStatusCode.InternalServerError, "Internal Server Error"}, // 500
            {(int)HttpStatusCode.NotImplemented, "Not Implemented"}, // 501
            {(int)HttpStatusCode.BadGateway, "Bad Gateway"}, // 502
            {(int)HttpStatusCode.ServiceUnavailable, "Service Unavailable"}, // 503
            {(int)HttpStatusCode.GatewayTimeout, "Gateway Timeout"}, // 504
            {(int)HttpStatusCode.HttpVersionNotSupported, "Http Version Not Supported"}, // 505

            {507, "Insufficient Storage"}, // 507
        };

        public static string GetMessage(int responseCode)
        {
            if (messages.ContainsKey(responseCode))
            {
                return messages[responseCode];
            }

            return "Unknown";
        }
    }
}
