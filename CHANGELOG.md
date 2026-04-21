# Histórico de Versões (Changelog)

Todas as mudanças notáveis neste projeto serão documentadas neste arquivo.

## [v3.0.4] - 2026-03-21

### 🐛 Correções de Bugs (Bug Fixes)
- **Avaliação de Galpão**: Corrigido o bug onde a lista de histórico de avaliações não era atualizada automaticamente ao salvar um novo registro.
- **Memory Leaks**: Implementação do padrão `IDisposable` e limpeza da memória nas telas transientes (Diagnóstico, Tratamento, Vacinas, Salmonella, Manejo, Zootécnico e Nutrição) para evitar travamentos ou consumo excessivo de memória ao navegar repetidamente.
- **Crash de Formulário**: Corrigido um fechamento inesperado (crash) do aplicativo que ocorria ao carregar os parâmetros de alguns lotes.
- **ISI Macro**: Corrigida a exibição dos parâmetros isolados no checklist, que antes não apareciam corretamente.

### ✨ Melhorias e Usabilidade (Enhancements)
- **Feedback Tátil ("Tremidinha")**: Totalmente reestruturado nos campos numéricos para aumentar a acessibilidade e percepção de acerto (`+` e `-`). Adicionada trava de segurança para não vibrar sucessivamente ao carregar registros do banco de dados na inicialização da tela.
- **Campos Numéricos mais Limpos**: Desabilitado o botão de limpar ("X") do sistema nos campos numéricos personalizados `SfNumericUpDown` para evitar digitação acidental.
- **Performance de UI**:
  - Removidos bindings redundantes de `IsVisible` com a propriedade `IsBusy` em diversas telas, trazendo um layout XAML mais limpo e fluído.
  - Implementação de instâncias Singleton (cache ativado) para certas views para carregamento quase instantâneo do app.
- **Títulos e Categorias**: Removidos os títulos redundantes da categoria nas visões do ISI Macro para mais espaço de tela produtivo.
- **Monitoramento e Dashboards**: Pequenos aprimoramentos para navegação direta a partir das opções e relatórios detalhados.

### 🛠 Manutenção e Infraestrutura
- Integração do **Sentry** aprimorada para enviar relatórios de erros mais completos caso o aplicativo sofra falhas durante a inicialização na plataforma Android/iOS.
- Atualização do controle de versão interno para compilação multiplataforma.
