using Aria;
using Azure.AI.OpenAI;
using Azure.Identity;
using dotenv.net;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Compaction;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Text.Json;

#pragma warning disable MAAI001

DotEnv.Load();

var endpoint= Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT")
                     ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT Not defined");
var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME")
                     ?? "gpt-5.4-mini";
                     
AzureOpenAIClient client = new AzureOpenAIClient(
    new Uri(endpoint),
    new DefaultAzureCredential());

//Definiçao dos agentes. Aqui estamos usando o mesmo modelo. Mas em produção da pra usar um modelo mais fraco em Produção.
//Exemplo: Voce utilizar pro Agent um gpt-5 e pro summarizer o gpt-5-mini
IChatClient agentChatClient = client.GetChatClient(deploymentName).AsIChatClient();
IChatClient summarizerChatClient = client.GetChatClient(deploymentName).AsIChatClient();

// Liga logs para enxergar quando cada etapa de compaction é aplicada ou ignorada.
using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        })
        .SetMinimumLevel(LogLevel.Debug)
        .AddFilter((category, level) =>
            category != null &&
            category.Contains("Microsoft.Agents.AI", StringComparison.OrdinalIgnoreCase) &&
            level >= LogLevel.Debug);
});

//Pipeline de compactação
// Estratégias encadeadas do menos agressivo para o mais agressivo.
// Cada etapa só é ativada quando um trigger for ativado.
PipelineCompactionStrategy compactionPipeline = new(
    // 1. Mais leve de todos - colapsa grupos de tool-calls antigos em resumos curtos.
    new ToolResultCompactionStrategy(CompactionTriggers.MessagesExceed(7)),
    
    // 2. Moderado - LLM menor que você utiliza para resumir as conversas mais antigas
    new SummarizationCompactionStrategy(summarizerChatClient, CompactionTriggers.TokensExceed(0x600)), // 1536 tokens

    // 3. Agressivo - mantém só os últimos 5 turnos do usuário
    new SlidingWindowCompactionStrategy(
        CompactionTriggers.TurnsExceed(5)),

    // 4. Emergência - corta mensagens antigas até caber no limite.
    new TruncationCompactionStrategy(CompactionTriggers.TokensExceed(0x800)) // 2048 tokens
);
   
   
//Criando nosso Agente
AIAgent aria = 
agentChatClient 
    .AsBuilder()
    .UseAIContextProviders(new CompactionProvider(compactionPipeline, stateKey: null, loggerFactory: loggerFactory))
    .BuildAIAgent(new ChatClientAgentOptions()
    {
        Name = "Aria",
        ChatOptions = new()
        {
            Instructions = """
                           Você é a Aria, uma assistente pessoal de produtividade altamente eficiente.
                           Você gerencia tarefas, reuniões e e-mails de um desenvolvedor de software chamado Lucas.
                           Seja concisa, objetiva e proativa — se perceber algo importante nos dados,
                           mencione mesmo que o usuário não perguntou diretamente.
                           Use sempre português brasileiro.
                           """,
            Tools = 
            [
                // ── Ferramentas de Tarefas (simulando Jira) ──
                AIFunctionFactory.Create(FakeTools.GetSprintTasks),
                AIFunctionFactory.Create(FakeTools.GetTaskDetails),
                AIFunctionFactory.Create(FakeTools.UpdateTaskStatus),
                AIFunctionFactory.Create(FakeTools.CreateTask),

                // ── Ferramentas de Calendário ──
                AIFunctionFactory.Create(FakeTools.GetTodayMeetings),
                AIFunctionFactory.Create(FakeTools.ScheduleMeeting),

                // ── Ferramentas de E-mail ──
                AIFunctionFactory.Create(FakeTools.GetUnreadEmails),
                AIFunctionFactory.Create(FakeTools.GetEmailDetails),
            ]
        }
    });
    
// Sessão de Conversa
AgentSession session = await aria.CreateSessionAsync();

// Helper: imprime o tamanho atual do histórico
void PrintHistoryStats()
{
    if (session.TryGetInMemoryChatHistory(out var history))
    {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n  [histórico: {history.Count} mensagens]\n");
        Console.ResetColor();
    }
}

void PrintSessionJsonSnapshot()
{
    Console.ForegroundColor = ConsoleColor.DarkCyan;
    Console.WriteLine("\n[SESSION JSON SNAPSHOT]");
    Console.ResetColor();

    if (!session.TryGetInMemoryChatHistory(out var history))
    {
        Console.WriteLine("{\"error\":\"No in-memory chat history available\"}");
        return;
    }

    var payload = new
    {
        messageCount = history.Count,
        messages = history.Select((message, index) => new
        {
            index,
            role = message.Role.ToString(),
            contents = message.Contents.Select(content => new
            {
                kind = content?.GetType().Name,
                text = content?.ToString()
            })
        })
    };

    Console.WriteLine(JsonSerializer.Serialize(payload, new JsonSerializerOptions
    {
        WriteIndented = true
    }));
}

// ── Conversa Simulada ────────────────────────────────────────
//
//  Os prompts imitam uma sessão real de trabalho do Lucas ao longo do dia.
//  Observe o contador de mensagens crescendo e depois sendo comprimido
//  pela compaction pipeline.
//
string[] prompts =
[
    // Manhã — início do dia
    "Aria, bom dia! O que tenho de reuniões hoje?",

    "Quais tasks estão na sprint atual? Me dá um resumo geral.",

    "Tem algum e-mail importante não lido que eu deva saber?",

    // Desenvolvimento — meio da manhã
    "Me dá os detalhes da task ARIA-42. Quero entender o que precisa ser feito.",

    "Ok, vou começar a trabalhar nisso. Muda o status da ARIA-42 para 'Em Progresso'.",

    "Quais tasks ainda estão como 'A Fazer' e têm alta prioridade?",

    // Almoço — retomada de contexto (teste de memória!)
    "Voltei do almoço. Me lembra: qual era o assunto do e-mail mais urgente que você " +
    "me mencionou de manhã?",

    // Tarde — novos trabalhos
    "Cria uma task nova: 'Revisar PR do Felipe para o módulo de autenticação', " +
    "prioridade alta, sprint atual.",

    "Agenda uma reunião amanhã às 14h com Felipe para discutir o PR. " +
    "Título: 'Code Review — Autenticação'.",

    // Final do dia
    "Me dá um resumo do que fizemos hoje. O que ficou pendente?",
];

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("╔══════════════════════════════════════════════════╗");
Console.WriteLine("║   Aria — Assistente Pessoal  |  Demo Compaction  ║");
Console.WriteLine("╚══════════════════════════════════════════════════╝");
Console.ResetColor();

foreach (var prompt in prompts)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("Lucas: ");
    Console.ResetColor();
    Console.WriteLine(prompt);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("\nAria:  ");
    Console.ResetColor();
    Console.WriteLine(await aria.RunAsync(prompt, session));

    PrintHistoryStats();
    Console.WriteLine(new string('─', 60));
}

PrintSessionJsonSnapshot();
