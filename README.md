<h1 align="center">
  <br>
  SilvaData 📱
  <br>
</h1>

<h4 align="center">Aplicativo robusto multiplataforma para gestão e avaliação avançada de lotes aviários, desenvolvido em <a href="https://dotnet.microsoft.com/en-us/apps/maui" target="_blank">.NET MAUI</a>.</h4>

<p align="center">
  <a href="#principais-funcionalidades">Funcionalidades</a> •
  <a href="#arquitetura-e-design">Arquitetura</a> •
  <a href="#boas-práticas-implementadas">Boas Práticas</a> •
  <a href="#pacotes-e-dependências">Dependências</a> •
  <a href="#instalação-e-execução">Execução</a>
</p>

---

## 📋 Visão Geral

O **SilvaData** é a evolução moderna e otimizada da aplicação de Avaliações e Diagnósticos de lotes em campo. Desenvolvido nativamente usando o framework **.NET MAUI**, ele opera graciosamente em plataformas **Android** e **iOS**, entregando performance quase instantânea, coleta de dados offline, além de uma interface limpa e responsiva voltada a técnicos de campo e veterinários. 

## 🌟 Principais Funcionalidades

* 📊 **Gestão Completa de Lotes**: Ferramentas paramétricas para avaliações amplas (Galpão, Nutrição, Zootécnico, Diagnósticos, Tratamento via Água e Vacinas).
* 📝 **Formulários Dinâmicos e Inteligentes**: Carregamento dinâmico do formulário por SQLite suportando dados qualitativos e quantitativos complexos.
* ⚡ **Alta Performance Offshore (Offline First)**: Armazenamento integral através do `sqlite-net-pcl` altamente otimizado para não gargalar o preenchimento, ideal para campo rural profundo.
* 📳 **Respostas Táteis Naturais**: Integração profunda de Haptic Feedback (vibração controlada) nos componentes interativos (ex. Modificadores Quantitativos) para garantir percepção de toque pelo operador usando luvas.
* 📸 **Capturas de Campo Multimídia**: Uso da câmera otimizado por `CommunityToolkit.Maui.Camera` para anexação performática de fotos aos formulários na fase de necrópsias.

---

## 🏗 Arquitetura e Design

Construído sob o rígido padrão **MVVM (Model-View-ViewModel)** com **Injeção de Dependências**, garantindo a fácil testabilidade e isolamento lógico.

* **Componentes de UI de Alta Fidelidade**: Implementação do conjunto `Syncfusion.Maui` (ListView, NumericUpDown, TabView) reajustados para oferecer interatividade premium e fluidez visual.
* **Mensageria Fraca (WeakReferenceMessenger)**: Para atualizações rápidas em tempo real entre camadas e abas sem criar acoplamento hard-coded (ex. `FormularioSalvoMessage`).
* **Singleton vs Transient Lifecycle**:  
  - Visões de Dashboard e Configurações pesadas operam como **Singletons** para garantir que a navegação do Shell ocorra em "zero ms".
  - Formulários dinâmicos de Lote funcionam como **Transient**, economizando recursos globais mediante destruição quando fechados.

---

## 🏆 Boas Práticas Implementadas

Garantimos que o SilvaData não seja apenas mais um aplicativo, mas de **Classificação Empresarial (Enterprise Grade)**. 

1. **Gestão Limpa de Vida Útil (GC Assurance) 🧹**: 
   Cada página descartável (*Transient*) que usa `WeakReferenceMessenger` obrigatoriamente implementa a interface `IDisposable`, anulando proativamente as assinaturas de memória ao sair da tela (*OnBackButtonPressed/PopModalAsync*). Acaba definitivamente com eventuais vazamentos de memória (Memory Leaks).
2. **Ciclo Silencioso no Banco de Dados 🔕**: 
   Introduzido o conceito de `IsLoadingData`. As rotinas de "hidratação de modelo" pelo banco SQLite agora agem silenciosamente, impedindo que interações UI ou vibrações haptics ativem desnecessariamente nos construtores em loop.
3. **Gerenciamento Ativo de Sentry 🚧**: 
   Reportes instantâneos de falha para a nuvem através do `Sentry.Maui`. O App está preparado para documentar não apenas Exceções Fatais não Tratadas (Unhandled), mas também capturar o fluxo do rastreamento de pilha (stack trace) em tempo real, priorizando resoluções rápidas de incidentes.
4. **Proteções e Clamping em Controls 🔒**: 
   Limpeza contextual de propriedades destrutivas e campos excessivos em UX – ex: Ocultação sistêmica de ClearButtons em entradas de números e "clamping" proativo impedindo crashs via Input incorreto.

---

## 📦 Pacotes e Dependências Core

| Pacote | Função Especial |
| --- | --- |
| `CommunityToolkit.Mvvm` | Data-Bindings ultraperformantes usando "Source Generator" para ViewModels (`@ObservableProperty` e `@RelayCommand`). |
| `CommunityToolkit.Maui` | Extensões nativas globais e popups cross-platform. |
| `Syncfusion.Maui.*` | Extensão essencial para grades (DataGrids), componentes de agendamento, seletores e NumericUpDowns industriais. |
| `sqlite-net-pcl` | ORM leve para mapear e interagir de forma brutalmente rápida com o banco relacional local. |
| `Sentry.Maui` | Observabilidade, rastreamento de falhas e análise de desempenho. |
| `LocalizationResourceManager`| Gestão traduzida universal baseada em ResX. |

---

## 🚀 Instalação e Execução

Para rodar este projeto localmente, assegure que você tenha o [.NET MAUI SDK](https://learn.microsoft.com/en-us/dotnet/maui/get-started/installation) devidamente configurado, o Visual Studio atualizado (com cargas C# e SDK Target Mobile adequadas) e execute:

```bash
# 1. Restaure aos pacotes as ferramentas (Se aplicável)
dotnet restore SilvaData.sln

# 2. Construa a aplicação (Recomenda-se via IDE para gerenciamento correto do Emulador iOS/Android)
```
*(Para iOS, possua as Chaves da Apple de Distribuição vinculadas à pasta Secrets ou use a conta Enterprise da CW-Software configurada via IDE)*.

## 🚀 Automação de Build (CI/CD)

Este projeto está integrado ao **Worker Central** da CW Software. O build é disparado automaticamente via Webhook do GitHub.

### Como funciona:
- **Trigger**: Qualquer `push` no branch `master`.
- **Script**: Localizado em `.cwbuild/build.sh`.
- **Monitoramento**: Acompanhe o status, telemetria e logs em tempo real no nosso [Dashboard de Builds](https://builds.cwsoftware.com.br).

### Artefatos:
Os arquivos `.aab` (assinados) e `.apk` de debug são gerados automaticamente e disponibilizados para download no portal após o sucesso da compilação.

---
> Elaborado por **Advanced Agentic AI Assistant** - Implementando excelência na migração de dados e ciclo MAUI. 🚀
