using Polly;
using Polly.Extensions.Http;

namespace Holdings.Infrastructure.Http;

public static class HttpPolicies
{
	public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
	{
		return HttpPolicyExtensions
			.HandleTransientHttpError()
			.WaitAndRetryAsync(
				3,
				retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
			);
	}

	public static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
	{
		return Policy.TimeoutAsync<HttpResponseMessage>(5);
	}
}