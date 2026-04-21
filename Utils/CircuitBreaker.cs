using SilvaData.Models;
using System.Diagnostics;

namespace SilvaData.Utils
{
    /// <summary>
    /// Estados do Circuit Breaker
    /// </summary>
    public enum CircuitState
    {
        Closed,    // Funcionando normal
        Open,      // Circuito aberto - bloqueia requisições
        HalfOpen   // Testando se serviço recuperou
    }

    /// <summary>
    /// Implementação do Circuit Breaker pattern para proteger contra falhas em cascata
    /// </summary>
    public class CircuitBreaker
    {
        private readonly object _lock = new object();
        private readonly string _serviceName;
        
        // Configurações
        private readonly int _failureThreshold;    // Número de falhas para abrir o circuito
        private readonly TimeSpan _openTimeout;     // Tempo que fica aberto
        private readonly TimeSpan _halfOpenTimeout; // Tempo de teste no estado half-open
        
        // Estado atual
        private CircuitState _state = CircuitState.Closed;
        private int _failureCount = 0;
        private DateTime _stateChanged = DateTime.UtcNow;
        private DateTime _lastSuccessTime = DateTime.UtcNow;
        
        // Estatísticas
        private int _totalRequests = 0;
        private int _successfulRequests = 0;
        private readonly List<TimeSpan> _responseTimes = new();

        public CircuitBreaker(string serviceName, int failureThreshold = 5, TimeSpan? openTimeout = null, TimeSpan? halfOpenTimeout = null)
        {
            _serviceName = serviceName ?? throw new ArgumentNullException(nameof(serviceName));
            _failureThreshold = failureThreshold;
            _openTimeout = openTimeout ?? TimeSpan.FromMinutes(1);
            _halfOpenTimeout = halfOpenTimeout ?? TimeSpan.FromSeconds(30);
        }

        /// <summary>
        /// Estado atual do circuit breaker
        /// </summary>
        public CircuitState State
        {
            get
            {
                lock (_lock)
                {
                    CheckStateTransition();
                    return _state;
                }
            }
        }

        /// <summary>
        /// Número atual de falhas consecutivas
        /// </summary>
        public int FailureCount
        {
            get
            {
                lock (_lock)
                {
                    return _failureCount;
                }
            }
        }

        /// <summary>
        /// Estatísticas de performance
        /// </summary>
        public (int Total, int Successful, double SuccessRate, TimeSpan AvgResponseTime) GetStatistics()
        {
            lock (_lock)
            {
                var successRate = _totalRequests > 0 ? (double)_successfulRequests / _totalRequests : 0;
                var avgResponseTime = _responseTimes.Any() 
                    ? TimeSpan.FromTicks((long)_responseTimes.Average(rt => rt.Ticks))
                    : TimeSpan.Zero;

                return (_totalRequests, _successfulRequests, successRate, avgResponseTime);
            }
        }

        /// <summary>
        /// Executa uma operação através do circuit breaker
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, Func<Task<T>>? fallback = null)
        {
            var startTime = DateTime.UtcNow;
            
            try
            {
                // Verifica se pode executar
                if (!CanExecute())
                {
                    throw new CircuitBreakerOpenException($"Circuit breaker aberto para serviço: {_serviceName}");
                }

                // Executa a operação
                var result = await operation();
                
                // Registra sucesso
                RecordSuccess(startTime);
                
                return result;
            }
            catch (Exception ex)
            {
                // Registra falha
                RecordFailure(startTime, ex);
                
                // Se tiver fallback, usa ele
                if (fallback != null && _state == CircuitState.Open)
                {
                    try
                    {
                        Debug.WriteLine($"[CircuitBreaker] Usando fallback para {_serviceName}");
                        return await fallback();
                    }
                    catch (Exception fallbackEx)
                    {
                        Debug.WriteLine($"[CircuitBreaker] Fallback falhou para {_serviceName}: {fallbackEx.Message}");
                        throw new CircuitBreakerOpenException($"Circuit breaker aberto e fallback falhou: {_serviceName}", fallbackEx);
                    }
                }
                
                throw;
            }
        }

        /// <summary>
        /// Verifica se pode executar uma operação
        /// </summary>
        public bool CanExecute()
        {
            lock (_lock)
            {
                CheckStateTransition();
                
                return _state switch
                {
                    CircuitState.Closed => true,
                    CircuitState.Open => false,
                    CircuitState.HalfOpen => true, // Permite uma requisição de teste
                    _ => false
                };
            }
        }

        /// <summary>
        /// Registra uma falha na operação
        /// </summary>
        private void RecordFailure(DateTime startTime, Exception exception)
        {
            lock (_lock)
            {
                _totalRequests++;
                _failureCount++;
                
                var responseTime = DateTime.UtcNow - startTime;
                _responseTimes.Add(responseTime);
                
                // Mantém apenas últimos 100 tempos de resposta
                if (_responseTimes.Count > 100)
                {
                    _responseTimes.RemoveAt(0);
                }

                Debug.WriteLine($"[CircuitBreaker] Falha registrada em {_serviceName}: {_failureCount}/{_failureThreshold} (Estado: {_state})");

                if (_state == CircuitState.Closed)
                {
                    if (_failureCount >= _failureThreshold)
                    {
                        TransitionToOpen();
                    }
                }
                else if (_state == CircuitState.HalfOpen)
                {
                    // Se falhar no estado half-open, volta para open
                    TransitionToOpen();
                }
            }
        }

        /// <summary>
        /// Registra um sucesso na operação
        /// </summary>
        private void RecordSuccess(DateTime startTime)
        {
            lock (_lock)
            {
                _totalRequests++;
                _successfulRequests++;
                _failureCount = 0; // Reset contador de falhas
                _lastSuccessTime = DateTime.UtcNow;
                
                var responseTime = DateTime.UtcNow - startTime;
                _responseTimes.Add(responseTime);
                
                // Mantém apenas últimos 100 tempos de resposta
                if (_responseTimes.Count > 100)
                {
                    _responseTimes.RemoveAt(0);
                }

                Debug.WriteLine($"[CircuitBreaker] Sucesso registrado em {_serviceName} (Estado: {_state})");

                if (_state == CircuitState.HalfOpen)
                {
                    // Se sucesso no half-open, volta para closed
                    TransitionToClosed();
                }
            }
        }

        /// <summary>
        /// Verifica se precisa transicionar de estado
        /// </summary>
        private void CheckStateTransition()
        {
            var now = DateTime.UtcNow;
            
            switch (_state)
            {
                case CircuitState.Open:
                    if (now - _stateChanged >= _openTimeout)
                    {
                        TransitionToHalfOpen();
                    }
                    break;
                    
                case CircuitState.HalfOpen:
                    if (now - _stateChanged >= _halfOpenTimeout)
                    {
                        // Se não teve sucesso no tempo de half-open, volta para open
                        TransitionToOpen();
                    }
                    break;
            }
        }

        /// <summary>
        /// Transiciona para estado Closed
        /// </summary>
        private void TransitionToClosed()
        {
            _state = CircuitState.Closed;
            _failureCount = 0;
            _stateChanged = DateTime.UtcNow;
            
            Debug.WriteLine($"[CircuitBreaker] {_serviceName}: CLOSED - Serviço recuperado");
        }

        /// <summary>
        /// Transiciona para estado Open
        /// </summary>
        private void TransitionToOpen()
        {
            _state = CircuitState.Open;
            _stateChanged = DateTime.UtcNow;
            
            Debug.WriteLine($"[CircuitBreaker] {_serviceName}: OPEN - {_failureCount} falhas detectadas");
        }

        /// <summary>
        /// Transiciona para estado Half-Open
        /// </summary>
        private void TransitionToHalfOpen()
        {
            _state = CircuitState.HalfOpen;
            _stateChanged = DateTime.UtcNow;
            
            Debug.WriteLine($"[CircuitBreaker] {_serviceName}: HALF-OPEN - Testando recuperação");
        }

        /// <summary>
        /// Reset manual do circuit breaker
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                TransitionToClosed();
                Debug.WriteLine($"[CircuitBreaker] {_serviceName}: RESET manual");
            }
        }

        /// <summary>
        /// Força o circuit breaker para estado Open (para testes)
        /// </summary>
        public void ForceOpen()
        {
            lock (_lock)
            {
                _failureCount = _failureThreshold;
                TransitionToOpen();
                Debug.WriteLine($"[CircuitBreaker] {_serviceName}: FORCED OPEN");
            }
        }
    }

    /// <summary>
    /// Exceção lançada quando o circuit breaker está aberto
    /// </summary>
    public class CircuitBreakerOpenException : Exception
    {
        public CircuitBreakerOpenException(string message) : base(message) { }
        public CircuitBreakerOpenException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Gerenciador de Circuit Breakers para múltiplos serviços
    /// </summary>
    public static class CircuitBreakerManager
    {
        private static readonly Dictionary<string, CircuitBreaker> _circuitBreakers = new();
        private static readonly object _lock = new object();

        /// <summary>
        /// Obtém ou cria um circuit breaker para um serviço
        /// </summary>
        public static CircuitBreaker GetCircuitBreaker(string serviceName, int failureThreshold = 5, TimeSpan? openTimeout = null, TimeSpan? halfOpenTimeout = null)
        {
            lock (_lock)
            {
                if (!_circuitBreakers.TryGetValue(serviceName, out var circuitBreaker))
                {
                    circuitBreaker = new CircuitBreaker(serviceName, failureThreshold, openTimeout, halfOpenTimeout);
                    _circuitBreakers[serviceName] = circuitBreaker;
                }
                
                return circuitBreaker;
            }
        }

        /// <summary>
        /// Obtém estatísticas de todos os circuit breakers
        /// </summary>
        public static Dictionary<string, (CircuitState State, int Failures, (int Total, int Successful, double SuccessRate, TimeSpan AvgResponseTime) Stats)> GetAllStatistics()
        {
            lock (_lock)
            {
                var result = new Dictionary<string, (CircuitState, int, (int, int, double, TimeSpan))>();
                
                foreach (var kvp in _circuitBreakers)
                {
                    var stats = kvp.Value.GetStatistics();
                    result[kvp.Key] = (kvp.Value.State, kvp.Value.FailureCount, stats);
                }
                
                return result;
            }
        }

        /// <summary>
        /// Reset todos os circuit breakers
        /// </summary>
        public static void ResetAll()
        {
            lock (_lock)
            {
                foreach (var circuitBreaker in _circuitBreakers.Values)
                {
                    circuitBreaker.Reset();
                }
            }
        }
    }
}
