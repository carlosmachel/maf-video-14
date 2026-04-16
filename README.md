
---

# 🧠 Compaction Pipeline — Conceitos (Agent Framework)

## 📌 Visão Geral

O **Compaction Pipeline** é uma estratégia para reduzir o histórico de conversas enviado para o LLM, mantendo o contexto essencial enquanto controla:

* Limite de tokens
* Custo de execução
* Latência

Sem compaction, cada chamada ao modelo inclui **todo o histórico**, o que rapidamente se torna inviável. ([Microsoft Learn][1])

---

## ⚠️ Quando usar

A compaction só funciona para:

* ✅ Agentes com **histórico em memória**
* ❌ NÃO funciona para:

    * Azure AI Foundry Agents
    * OpenAI Responses API com store
    * Copilot Studio

Porque nesses casos o contexto já é gerenciado pelo serviço. ([Microsoft Learn][1])

---

## 🧱 Estrutura Base

### 📦 MessageIndex

Representa o histórico estruturado da conversa.

* Agrupa mensagens em unidades chamadas `MessageGroup`
* Cada grupo tem:

    * Contagem de mensagens
    * Estimativa de tokens
    * Bytes

---

### 🧩 MessageGroup

Unidade atômica de mensagens que devem ser mantidas ou removidas juntas.

Tipos:

* `System` → sempre preservado
* `User` → início de um turno
* `AssistantText` → resposta simples
* `ToolCall` → chamada + resultado (atômico)
* `Summary` → resultado de sumarização

👉 Importante: não dá pra remover só metade de um grupo (ex: tool call sem resultado).

---

## 🚦 Triggers e Targets

### 🔥 Trigger

Define **quando iniciar** a compactação.

Exemplo:

```csharp
TokensExceed(32000)
```

---

### 🎯 Target

Define **quando parar** a compactação.

* Estratégia remove grupos gradualmente
* Para quando o target é satisfeito

👉 Se não definido:

* Target = inverso do trigger

---

## 🧠 Estratégias de Compaction

### 1. ✂️ Truncation

Remove mensagens antigas.

* Simples e agressivo
* Ideal como fallback

---

### 2. 🪟 Sliding Window

Mantém apenas os últimos N turns.

* Baseado em turnos (User + respostas)
* Mantém contexto recente

---

### 3. 🔧 Tool Result Compaction

Colapsa resultados de tools.

Exemplo:

```
[Tool results: get_weather: sunny]
```

* Mantém rastreabilidade
* Reduz bastante tokens

---

### 4. 🧾 Summarization

Usa um LLM para resumir histórico antigo.

* Preserva contexto importante
* Requer outro modelo (mais barato recomendado)

---

### 5. 🎯 Selective Tool Call

Remove completamente tool calls antigos.

* Mais agressivo que o anterior

---

### 6. 🧮 Token Budget Strategy

Pipeline orientado a orçamento de tokens.

* Executa estratégias até atingir o limite
* Possui fallback automático

---

## 🔗 PipelineCompactionStrategy

O coração do **Step18**.

Permite combinar várias estratégias em sequência:

```csharp
PipelineCompactionStrategy pipeline = new(
    new ToolResultCompactionStrategy(...),
    new SummarizationCompactionStrategy(...),
    new SlidingWindowCompactionStrategy(...),
    new TruncationCompactionStrategy(...)
);
```

### 🔄 Ordem importa

As estratégias são executadas em ordem:

1. 🟢 ToolResult (leve)
2. 🟡 Summarization (moderado)
3. 🟠 SlidingWindow (agressivo)
4. 🔴 Truncation (emergência)

👉 Isso cria uma **degradação progressiva do contexto**

---

## 🧠 Como o Pipeline funciona

* Sempre executa (`Trigger = Always`)
* Cada estratégia decide se roda ou não
* Output de uma vira input da próxima

👉 Modelo mental:

```
Raw History
   ↓
Tool Compaction
   ↓
Summarization
   ↓
Window Limit
   ↓
Hard Cut (fallback)
```

---

## 🔌 Integração com o Agent

A compaction é plugada via:

### `CompactionProvider`

```csharp
.UseAIContextProviders(new CompactionProvider(pipeline))
```

* Executa antes de cada chamada ao LLM
* Atua dentro do loop de tool-calling

---

## 🧠 Insights importantes (nível arquiteto)

### 1. Compaction = Memory Strategy

Isso é basicamente:

> “Como eu gerencio memória de curto prazo do agente?”

---

### 2. Pipeline = Graceful Degradation

Você não perde contexto de uma vez.

* Primeiro compacta
* Depois resume
* Só depois remove

---

### 3. Summarization ≠ Lossless

* Você troca precisão por eficiência
* Precisa de prompt bem desenhado

---

### 4. Tool Calls são críticos

* Não podem ser quebrados
* São tratados como unidades atômicas

---

### 5. Token Budget é o driver real

Tudo gira em torno de:

> 💰 custo + limite de contexto

---

## 🧪 Exemplo de Estratégia Real

```csharp
pipeline = new(
    ToolResultCompactionStrategy,
    SummarizationCompactionStrategy,
    SlidingWindowCompactionStrategy,
    TruncationCompactionStrategy
);
```

💡 Esse é o padrão recomendado:

* Otimiza custo
* Mantém contexto útil
* Evita erros de limite

---

## 🚀 TL;DR

* Compaction resolve **token limit + custo + latência**
* Funciona só para **in-memory agents**
* Usa **MessageGroups** como unidade
* Pipeline aplica estratégias em camadas
* Ordem das estratégias é crítica
* Summarization é poderosa, mas com trade-offs

---
[1]: https://learn.microsoft.com/en-us/agent-framework/agents/conversations/compaction?utm_source=chatgpt.com "Compaction | Microsoft Learn"
