using System.Diagnostics;

namespace SilvaData.Models
{
    /// <summary>
    /// Tipos de erro para tratamento diferenciado
    /// </summary>
    public enum ErrorType
    {
        Unknown,
        NetworkError,
        TimeoutError,
        ServerError,
        AuthenticationError,
        SerializationError,
        DecryptionError,
        BusinessLogicError,
        HtmlError,
        CircuitBreakerOpen
    }

    /// <summary>
    /// Detalhes completos de um erro para logging e exibição
    /// </summary>
    public class ErrorDetails
    {
        public string RequestId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public ErrorType ErrorType { get; set; } = ErrorType.Unknown;
        public string MethodName { get; set; } = string.Empty;
        public string ExceptionMessage { get; set; } = string.Empty;
        public string ExceptionType { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public string UserFriendlyMessage { get; set; } = string.Empty;
        
        // Informações da requisição
        public string RequestUrl { get; set; } = string.Empty;
        public string RequestPayload { get; set; } = string.Empty;
        public string RequestPayloadDecrypted { get; set; } = string.Empty;
        public Dictionary<string, string> RequestHeaders { get; set; } = new();
        
        // Informações da resposta
        public int? StatusCode { get; set; }
        public string ResponseContent { get; set; } = string.Empty;
        public string ResponseHeaders { get; set; } = string.Empty;
        public TimeSpan? ResponseTime { get; set; }
        
        // Informações do usuário
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? DeviceId { get; set; }
        
        // Contexto adicional
        public Dictionary<string, object> Context { get; set; } = new();
        public int RetryCount { get; set; } = 0;
        public bool IsRetryable { get; set; } = true;
        
        /// <summary>
        /// Indica se o erro deve ser ignorado no Sentry (erros de rede esperados)
        /// </summary>
        public bool ShouldIgnoreInSentry { get; set; } = false;
        
        /// <summary>
        /// Gera uma representação resumida para logs
        /// </summary>
        public string ToSummary()
        {
            return $"[{RequestId}] {MethodName} - {ErrorType}: {ExceptionMessage}";
        }
        
        /// <summary>
        /// Gera uma representação completa para debugging
        /// </summary>
        public string ToFullDetails()
        {
            var details = new System.Text.StringBuilder();
            
            details.AppendLine($"=== ERRO DETALHADO ===");
            details.AppendLine($"Request ID: {RequestId}");
            details.AppendLine($"Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss} UTC");
            details.AppendLine($"Error Type: {ErrorType}");
            details.AppendLine($"Method: {MethodName}");
            details.AppendLine($"Exception: {ExceptionType}");
            details.AppendLine($"Message: {ExceptionMessage}");
            details.AppendLine($"User Message: {UserFriendlyMessage}");
            details.AppendLine($"Retry Count: {RetryCount}");
            details.AppendLine($"Is Retryable: {IsRetryable}");
            
            if (!string.IsNullOrEmpty(RequestUrl))
            {
                details.AppendLine();
                details.AppendLine("--- REQUEST ---");
                details.AppendLine($"URL: {RequestUrl}");
                details.AppendLine($"Status Code: {StatusCode}");
                
                if (RequestHeaders.Any())
                {
                    details.AppendLine("Headers:");
                    foreach (var header in RequestHeaders)
                        details.AppendLine($"  {header.Key}: {header.Value}");
                }
                
                if (!string.IsNullOrEmpty(RequestPayload))
                {
                    details.AppendLine($"Payload (encrypted): {RequestPayload}");
                }
                
                if (!string.IsNullOrEmpty(RequestPayloadDecrypted))
                {
                    details.AppendLine($"Payload (decrypted): {RequestPayloadDecrypted}");
                }
            }
            
            if (!string.IsNullOrEmpty(ResponseContent))
            {
                details.AppendLine();
                details.AppendLine("--- RESPONSE ---");
                details.AppendLine($"Content Length: {ResponseContent.Length}");
                details.AppendLine($"Content: {ResponseContent}");
            }
            
            if (!string.IsNullOrEmpty(StackTrace))
            {
                details.AppendLine();
                details.AppendLine("--- STACK TRACE ---");
                details.AppendLine(StackTrace);
            }
            
            if (Context.Any())
            {
                details.AppendLine();
                details.AppendLine("--- CONTEXT ---");
                foreach (var kvp in Context)
                    details.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            
            details.AppendLine($"==================");
            
            return details.ToString();
        }
    }

    /// <summary>
    /// Resultado de uma tentativa de requisição com informações de erro
    /// </summary>
    public class RequestResult
    {
        public bool Success { get; set; }
        public ErrorDetails? ErrorDetails { get; set; }
        public string? Data { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public int AttemptCount { get; set; } = 1;
        
        public static RequestResult Successful(string data, TimeSpan executionTime, int attemptCount = 1)
        {
            return new RequestResult
            {
                Success = true,
                Data = data,
                ExecutionTime = executionTime,
                AttemptCount = attemptCount
            };
        }
        
        public static RequestResult Failed(ErrorDetails errorDetails, TimeSpan executionTime, int attemptCount = 1)
        {
            return new RequestResult
            {
                Success = false,
                ErrorDetails = errorDetails,
                ExecutionTime = executionTime,
                AttemptCount = attemptCount
            };
        }
    }
}
