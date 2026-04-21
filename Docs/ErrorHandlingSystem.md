# Sistema Avançado de Tratamento de Erros do WebService

## Overview

Sistema completo para tratamento de erros do ISI WebService com logging detalhado, circuit breaker e popups informativos.

## Componentes

### 1. Modelos de Erro (`ErrorDetails.cs`)
- **ErrorType**: Enumeração com tipos específicos de erro
- **ErrorDetails**: Classe com informações completas do erro
- **RequestResult**: Padronização de resultados de requisição

### 2. Logger Unificado (`ErrorLogger.cs`)
- Logging múltiplos destinos (Debug, arquivo, Sentry)
- Cache em memória para consulta recente
- Limpeza automática de logs antigos
- Exportação completa de logs

### 3. Circuit Breaker (`CircuitBreaker.cs`)
- Estados: CLOSED, OPEN, HALF-OPEN
- Proteção contra falhas em cascata
- Estatísticas de performance
- Fallback automático

### 4. Popups Melhorados
- **HtmlErrorPopup**: Com botão "Copiar HTML" e metadados
- **DetailedErrorPopup**: Popup completo para erros técnicos

## Uso

### Para Desenvolvedores

#### Logging de Erros
```csharp
// Usando o sistema automático (recomendado)
var errorDetails = ErrorLogger.CreateFromException(
    exception, 
    "methodName", 
    "requestId",
    requestUrl: "https://...",
    requestPayload: "encrypted_data",
    statusCode: 500);

await ErrorLogger.LogErrorAsync(errorDetails);
```

#### Circuit Breaker
```csharp
// Verificar status
var (state, failures, stats) = webService.GetCircuitBreakerStats();

// Reset manual (se necessário)
webService.ResetCircuitBreaker();
```

#### Diagnóstico
```csharp
// Executar diagnóstico completo
var diagnostic = await webService.RunDiagnosticAsync();
Debug.WriteLine(diagnostic);

// Manutenção automática
await webService.PerformMaintenanceAsync();
```

### Para Usuários Finais

#### Quando Ocorrer Erro HTML
1. Popup aparece com badge "HTML"
2. Botão **"Copiar HTML"** copia conteúdo completo para clipboard
3. Botão **"Compartilhar"** salva arquivo HTML para compartilhamento
4. Botão **"Fechar"** fecha popup

#### Para Outros Erros
1. Popup detalhado com informações técnicas
2. Botão **"Copiar"** copia todos os detalhes para clipboard
3. Botão **"Compartilhar"** salva arquivo .txt com detalhes
4. Accordion **"Ver Detalhes Técnicos"** mostra stack trace, payload, etc.

## Configuração

### Circuit Breaker
- **Failure Threshold**: 5 falhas para abrir
- **Open Timeout**: 2 minutos
- **Half-Open Timeout**: 30 segundos

### Error Logger
- **Memory Cache**: 100 erros recentes
- **File Retention**: 7 dias
- **Log Directory**: `FileSystem.CacheDirectory/error_logs/`

### Image Cache
- **Retention**: 7 dias
- **Formats**: .jpg, .png, .jpeg

## Melhorias Implementadas

### ✅ Visibilidade Completa
- Todos os erros com contexto completo
- Stack trace, payload, resposta
- Informações do usuário e dispositivo

### ✅ Recuperação Inteligente
- Circuit breaker protege contra falhas em cascata
- Retry específico por tipo de erro
- Fallback automático

### ✅ UX Melhorada
- Mensagens amigáveis e contextuais
- Popups detalhados com opções de compartilhamento
- Botão "Copiar HTML" para fácil compartilhamento

### ✅ Debugging Facilitado
- Logs detalhados em múltiplos formatos
- Exportação completa para análise
- Diagnóstico integrado

## Troubleshooting

### Logs Não Aparecendo
1. Verifique se `ErrorLogger.LogErrorAsync()` está sendo chamado
2. Confirme permissões de escrita no diretório de cache
3. Use `webService.RunDiagnosticAsync()` para verificar status

### Circuit Breaker Sempre Aberto
1. Verifique falhas consecutivas com `GetCircuitBreakerStats()`
2. Use `ResetCircuitBreaker()` para reset manual
3. Verifique conectividade de rede

### Popups Não Exibem
1. Confirme que `MainThread.InvokeOnMainThreadAsync` está sendo usado
2. Verifique se NavigationUtils está funcionando
3. Teste com PopUpOK.ShowAsync() como fallback

## Monitoramento

### Métricas Importantes
- Taxa de sucesso do circuit breaker
- Frequência de cada tipo de erro
- Tempo médio de resposta
- Uso de memória dos logs

### Alertas Recomendados
- Circuit breaker aberto > 5 minutos
- Taxa de erro > 20%
- Mais de 10 erros por minuto

## Futuras Melhorias

- Dashboard em tempo real
- Análise preditiva de falhas
- Integração com monitoring tools
- Auto-recuperação avançada
