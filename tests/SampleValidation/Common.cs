using System.Text;
using Xunit.Abstractions;

namespace SampleValidation
{
    class LoggingHandler : DelegatingHandler
    {
        readonly ITestOutputHelper outputHelper;

        public LoggingHandler(ITestOutputHelper outputHelper, HttpMessageHandler? innerHandler = null)
            : base(innerHandler ?? new HttpClientHandler())
        {
            this.outputHelper = outputHelper;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Log request information
            StringBuilder sb = new();
            sb.AppendLine($"Sending HTTP request:").AppendLine($"{request.Method} {request.RequestUri}");
            foreach (var header in request.Headers)
            {
                sb.AppendLine($"{header.Key}: {string.Join(",", header.Value)}");
            }

            if (request.Content != null)
            {
                foreach (var header in request.Content.Headers)
                {
                    sb.AppendLine($"{header.Key}: {string.Join(",", header.Value)}");
                }

                string requestContent = await request.Content.ReadAsStringAsync(cancellationToken);
                sb.AppendLine().AppendLine(requestContent);
            }

            this.outputHelper.WriteLine(sb.ToString());

            // Send the request and capture the response
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

            // Log response information
            sb.Clear();
            sb.AppendLine($"Got HTTP response:").AppendLine($"{(int)response.StatusCode} {response.ReasonPhrase}");
            foreach (var header in response.Headers)
            {
                sb.AppendLine($"{header.Key}: {string.Join(",", header.Value)}");
            }

            if (response.Content != null)
            {
                foreach (var header in response.Content.Headers)
                {
                    sb.AppendLine($"{header.Key}: {string.Join(",", header.Value)}");
                }

                if (response.Content.Headers.ContentLength != 0)
                {
                    string responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    sb.AppendLine().AppendLine(responseContent);
                }
            }

            this.outputHelper.WriteLine(sb.ToString());

            return response;
        }
    }
}
