namespace TodoTaskAPI.API.Middleware
{
    /// <summary>
    /// Middleware that implements token bucket algorithm for API rate limiting.
    /// Restricts the number of requests a client can make within a specified time interval.
    /// </summary>
    /// <remarks>
    /// Configuration parameters:
    /// - MaxBucketTokens: Maximum number of tokens a client can accumulate
    /// - TokensPerInterval: Number of tokens added per interval
    /// - IntervalSeconds: Time interval for token replenishment
    /// - MaxWaitTimeMs: Maximum time to wait for a token
    /// 
    /// Rate limiting is applied per client IP address.
    /// When limit is exceeded, returns HTTP 429 (Too Many Requests).
    /// </remarks>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly Dictionary<string, TokenBucket> _buckets = new();
        private readonly object _lock = new();

        // Configuration values
        private const int MaxBucketTokens = 10;       // Maximum 10 concurrent requests
        private const int TokensPerInterval = 5;     // 5 requests per interval
        private const int IntervalSeconds = 1;        // 1 second interval
        private const int MaxWaitTimeMs = 1000;       // 1 second maximum wait     

        /// <summary>
        /// Constructor for the rate limiting middleware.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">Logger for recording middleware activity.</param>
        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Processes an HTTP request applying rate limiting rules
        /// </summary>
        /// <param name="context">The HTTP context</param>
        public async Task InvokeAsync(HttpContext context)
        {
            var clientId = GetClientIdentifier(context);

            // Log the state of the token bucket before attempting to acquire a token
            LogBucketState(clientId);

            if (!await TryAcquireTokenAsync(clientId))
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId}", clientId);
                context.Response.StatusCode = StatusCodes.Status429TooManyRequests;

                // Add standard rate limiting headers to the response
                context.Response.Headers.Append("Retry-After", IntervalSeconds.ToString());
                context.Response.Headers.Append("X-RateLimit-Limit", TokensPerInterval.ToString());
                context.Response.Headers.Append("X-RateLimit-Reset",
                    DateTimeOffset.UtcNow.AddSeconds(IntervalSeconds).ToString("R"));

                // Return a JSON response indicating the rate limit has been exceeded
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Too many requests. Please try again later.",
                    retryAfter = IntervalSeconds
                });
                return;
            }
            
            // Add rate limit information to response headers
            var bucket = GetBucket(clientId);
            context.Response.Headers.Append("X-RateLimit-Remaining", bucket.CurrentTokens.ToString());

            await _next(context);
        }

        /// <summary>
        /// Generates a unique identifier for the client based on their IP address.
        /// </summary>
        /// <param name="context">The HTTP context for the current request.</param>
        /// <returns>A string representing the client identifier.</returns>
        private string GetClientIdentifier(HttpContext context)
        {
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        /// <summary>
        /// Retrieves or creates a token bucket for the specified client.
        /// </summary>
        /// <param name="clientId">The unique identifier of the client.</param>
        /// <returns>The token bucket associated with the client.</returns>
        private TokenBucket GetBucket(string clientId)
        {
            lock (_lock)
            {
                if (!_buckets.TryGetValue(clientId, out var bucket))
                {
                    bucket = new TokenBucket(MaxBucketTokens, TokensPerInterval, IntervalSeconds);
                    _buckets[clientId] = bucket;
                }
                return bucket;
            }
        }

        /// <summary>
        /// Logs the state of the token bucket for the specified client.
        /// </summary>
        /// <param name="clientId">The unique identifier of the client.</param>
        private void LogBucketState(string clientId)
        {
            var bucket = GetBucket(clientId);
            _logger.LogInformation(
                "Bucket state for {ClientId}: {State}",
                clientId,
                bucket.GetStatus());
        }

        /// <summary>
        /// Attempts to acquire a token from the client's token bucket.
        /// </summary>
        /// <param name="clientId">The unique identifier of the client.</param>
        /// <returns>True if a token is acquired successfully, otherwise false.</returns>
        private async Task<bool> TryAcquireTokenAsync(string clientId)
        {
            var bucket = GetBucket(clientId);
            return await bucket.TryAcquireAsync(MaxWaitTimeMs);
        }

        /// <summary>
        /// Implements a token bucket algorithm for rate limiting
        /// </summary>
        private class TokenBucket
        {
            private readonly int _maxTokens;               // Maximum tokens the bucket can hold
            private readonly int _tokensPerInterval;       // Tokens added per interval
            private readonly TimeSpan _interval;           // Time interval for replenishment
            private double _currentTokens;                 // Current token count
            private DateTime _lastRefillTime;              // Last token refill timestamp
            private readonly object _lock = new();         // Lock for thread safety

            /// <summary>
            /// Gets the current number of tokens in the bucket.
            /// </summary>
            public double CurrentTokens => _currentTokens;

            /// <summary>
            /// Initializes a new instance of the TokenBucket class.
            /// </summary>
            /// <param name="maxTokens">Maximum tokens the bucket can hold.</param>
            /// <param name="tokensPerInterval">Tokens added per interval.</param>
            /// <param name="intervalSeconds">Time interval for replenishment in seconds.</param>
            public TokenBucket(int maxTokens, int tokensPerInterval, int intervalSeconds)
            {
                _maxTokens = maxTokens;
                _tokensPerInterval = tokensPerInterval;
                _interval = TimeSpan.FromSeconds(intervalSeconds);
                _currentTokens = maxTokens;
                _lastRefillTime = DateTime.UtcNow;
            }

            /// <summary>
            /// Returns the current status of the token bucket as a string.
            /// </summary>
            /// <returns>A string representing the current status of the bucket.</returns>
            public string GetStatus()
            {
                lock (_lock)
                {
                    return $"Current tokens: {_currentTokens}/{_maxTokens}, Last refill: {_lastRefillTime:HH:mm:ss.fff}";
                }
            }

            /// <summary>
            /// Attempts to acquire a token from the bucket.
            /// </summary>
            /// <param name="maxWaitTimeMs">Maximum time to wait for a token in milliseconds.</param>
            /// <returns>True if a token is acquired successfully, otherwise false.</returns>
            public async Task<bool> TryAcquireAsync(int maxWaitTimeMs)
            {
                var waitTime = 0;
                while (waitTime < maxWaitTimeMs)
                {
                    lock (_lock)
                    {
                        RefillTokens();
                        if (_currentTokens >= 1)
                        {
                            _currentTokens--;
                            return true;
                        }
                    }

                    await Task.Delay(100);
                    waitTime += 100;
                }

                return false;
            }

            /// <summary>
            /// Refills the bucket with tokens based on the elapsed time.
            /// </summary>
            private void RefillTokens()
            {
                var now = DateTime.UtcNow;
                var timePassed = (now - _lastRefillTime).TotalSeconds;
                var tokensToAdd = timePassed * _tokensPerInterval / _interval.TotalSeconds;

                if (tokensToAdd >= 1)
                {
                    _currentTokens = Math.Min(_maxTokens, _currentTokens + tokensToAdd);
                    _lastRefillTime = now;
                }
            }
        }
    }
}
