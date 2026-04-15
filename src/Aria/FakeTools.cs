// ============================================================
//  FakeTools.cs — Ferramentas que simulam integrações reais
//
//  Todos os dados são estáticos e fictícios.
//  Em produção: substitua cada método por uma chamada real
//  à API correspondente (Jira REST API, Google Calendar, etc.)
// ============================================================

using System.ComponentModel;

namespace Aria;

public static class FakeTools
{
    // ── BANCO DE DADOS FAKE ──────────────────────────────────

    private static readonly List<Task> Tasks =
    [
        new("ARIA-38", "Implementar autenticação OAuth2",          "Em Progresso", "Alta",    "Lucas",  "Sprint 12"),
        new("ARIA-39", "Escrever testes unitários para UserService","A Fazer",     "Alta",    "Lucas",  "Sprint 12"),
        new("ARIA-40", "Atualizar documentação da API REST",        "A Fazer",     "Média",   "Lucas",  "Sprint 12"),
        new("ARIA-41", "Corrigir bug no endpoint de logout",        "Concluído",   "Alta",    "Lucas",  "Sprint 12"),
        new("ARIA-42", "Migrar banco de dados para PostgreSQL",     "A Fazer",     "Alta",    "Lucas",  "Sprint 12"),
        new("ARIA-43", "Refatorar camada de repositório",           "A Fazer",     "Baixa",   "Lucas",  "Sprint 12"),
        new("ARIA-44", "Configurar pipeline de CI/CD no GitHub",    "Em Progresso","Média",   "Felipe", "Sprint 12"),
        new("ARIA-45", "Revisar PR de autenticação com OAuth",      "A Fazer",     "Alta",    "Felipe", "Sprint 12"),
    ];

    private static readonly List<Meeting> Meetings =
    [
        new("Daily Standup",               "09:00", "09:15", ["Lucas", "Felipe", "Ana", "Pedro"]),
        new("Planning Sprint 13",          "10:00", "11:30", ["Lucas", "Felipe", "Ana", "Pedro", "Carla"]),
        new("1:1 com Tech Lead (Carla)",   "15:00", "15:30", ["Lucas", "Carla"]),
    ];

    private static readonly List<Email> Emails =
    [
        new("felipe@empresa.com",   "Felipe",    "PR aberto — módulo autenticação",
            "Oi Lucas, abri o PR #87 com a implementação do OAuth2. Preciso de review até amanhã pois vai para produção na sexta. Dá uma olhada quando puder?",
            Urgente: true),
        new("carla@empresa.com",    "Carla",     "Sprint 13 — prioridades",
            "Pessoal, para o Sprint 13 vamos priorizar a migração do banco e os testes do UserService. Por favor confirme a estimativa até o final do dia.",
            Urgente: true),
        new("noreply@github.com",   "GitHub",    "CI falhou — branch main",
            "A pipeline de CI falhou no branch main. O teste TaskServiceTest.ShouldCreateTask está quebrando. Verifique os logs em: github.com/empresa/projeto/actions",
            Urgente: false),
        new("pedro@empresa.com",    "Pedro",     "Almoço hoje?",
            "E aí, bora almoçar hoje ao meio-dia no restaurante do térreo?",
            Urgente: false),
    ];

    private static readonly Dictionary<string, string> TaskDetails = new()
    {
        ["ARIA-42"] = """
            Título:      Migrar banco de dados para PostgreSQL
            Sprint:      Sprint 12
            Prioridade:  Alta
            Responsável: Lucas
            Status:      A Fazer
            Estimativa:  5 story points

            Descrição:
            Atualmente o sistema usa SQL Server. Esta task cobre a migração
            completa para PostgreSQL 15, incluindo:
              • Adaptar as migrations do Entity Framework Core
              • Atualizar a string de conexão nos ambientes de dev, staging e prod
              • Verificar compatibilidade de tipos (especialmente JSON e UUID)
              • Executar testes de regressão completos após a migração
              • Atualizar o README com instruções de setup local

            Critérios de aceite:
              ✓ Todos os testes de integração passando com PostgreSQL
              ✓ Deploy em staging validado pelo QA
              ✓ Zero downtime durante a migração em produção (usar blue-green)

            Bloqueadores: nenhum
            Dependências: ARIA-38 (deve estar concluída antes do deploy)
            """,
    };

    // ── FERRAMENTAS DE TAREFAS (simulando Jira) ──────────────

    [Description("Retorna todas as tasks da sprint atual, com status e prioridade.")]
    public static string GetSprintTasks(
        [Description("Filtrar por status específico. Valores aceitos: 'A Fazer', 'Em Progresso', 'Concluído'. Deixe vazio para retornar todas.")] string statusFilter = "")
    {
        var tasks = string.IsNullOrWhiteSpace(statusFilter)
            ? Tasks
            : Tasks.Where(t => t.Status.Equals(statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        if (tasks.Count == 0)
            return $"Nenhuma task encontrada com status '{statusFilter}'.";

        var linhas = tasks.Select(t =>
            $"[{t.Id}] {t.Titulo} — Status: {t.Status} | Prioridade: {t.Prioridade} | Responsável: {t.Responsavel}");

        return $"Sprint 12 — {tasks.Count} task(s) encontrada(s):\n\n" + string.Join("\n", linhas);
    }

    [Description("Retorna os detalhes completos de uma task específica pelo ID (ex: ARIA-42).")]
    public static string GetTaskDetails(
        [Description("ID da task no formato ARIA-NN")] string taskId)
    {
        if (TaskDetails.TryGetValue(taskId.ToUpper(), out var detalhe))
            return detalhe;

        var task = Tasks.FirstOrDefault(t => t.Id.Equals(taskId, StringComparison.OrdinalIgnoreCase));
        return task is null 
            ? $"Task '{taskId}' não encontrada." 
            : $"[{task.Id}] {task.Titulo}\nStatus: {task.Status} | Prioridade: {task.Prioridade} | Responsável: {task.Responsavel}\n(Detalhes completos não disponíveis para esta task.)";
    }

    [Description("Atualiza o status de uma task.")]
    public static string UpdateTaskStatus(
        [Description("ID da task (ex: ARIA-42)")] string taskId,
        [Description("Novo status. Valores aceitos: 'A Fazer', 'Em Progresso', 'Concluído'")] string novoStatus)
    {
        var task = Tasks.FirstOrDefault(t => t.Id.Equals(taskId, StringComparison.OrdinalIgnoreCase));
        if (task is null)
            return $"Task '{taskId}' não encontrada.";

        var statusAnterior = task.Status;
        task.Status = novoStatus;
        return $"Task {taskId} atualizada: '{statusAnterior}' → '{novoStatus}'. Alteração registrada às {DateTime.Now:HH:mm}.";
    }

    [Description("Cria uma nova task na sprint atual.")]
    public static string CreateTask(
        [Description("Título da nova task")] string titulo,
        [Description("Prioridade: 'Alta', 'Média' ou 'Baixa'")] string prioridade = "Média",
        [Description("Responsável pela task")] string responsavel = "Lucas")
    {
        var novoId = $"ARIA-{46 + Tasks.Count(t => t.Id.StartsWith("ARIA-4"))}";
        var novaTask = new Task(novoId, titulo, "A Fazer", prioridade, responsavel, "Sprint 12");
        Tasks.Add(novaTask);
        return $"Task criada com sucesso!\nID: {novoId}\nTítulo: {titulo}\nPrioridade: {prioridade}\nResponsável: {responsavel}\nSprint: Sprint 12\nStatus: A Fazer";
    }

    // ── FERRAMENTAS DE CALENDÁRIO ────────────────────────────

    [Description("Retorna as reuniões agendadas para hoje.")]
    public static string GetTodayMeetings()
    {
        var hoje = DateTime.Now.ToString("dd/MM/yyyy");
        var linhas = Meetings.Select(m =>
            $"• {m.Inicio}–{m.Fim}  |  {m.Titulo}\n  Participantes: {string.Join(", ", m.Participantes)}");

        return $"Reuniões de hoje ({hoje}):\n\n" + string.Join("\n\n", linhas);
    }

    [Description("Agenda uma nova reunião.")]
    public static string ScheduleMeeting(
        [Description("Título da reunião")] string titulo,
        [Description("Data no formato DD/MM/YYYY")] string data,
        [Description("Horário de início no formato HH:mm")] string horarioInicio,
        [Description("Horário de término no formato HH:mm")] string horarioFim = "",
        [Description("Lista de participantes separados por vírgula")] string participantes = "")
    {
        var fim = string.IsNullOrWhiteSpace(horarioFim)
            ? $"{int.Parse(horarioInicio.Split(':')[0]) + 1:D2}:{horarioInicio.Split(':')[1]}"
            : horarioFim;

        var parts = string.IsNullOrWhiteSpace(participantes)
            ? ["Lucas"]
            : participantes.Split(',').Select(p => p.Trim()).ToList();

        Meetings.Add(new Meeting(titulo, horarioInicio, fim, parts));

        return $"Reunião agendada!\n" +
               $"Título:        {titulo}\n" +
               $"Data:          {data}\n" +
               $"Horário:       {horarioInicio}–{fim}\n" +
               $"Participantes: {string.Join(", ", parts)}\n" +
               $"Convite enviado para todos os participantes.";
    }

    // ── FERRAMENTAS DE E-MAIL ────────────────────────────────

    [Description("Retorna um resumo dos e-mails não lidos, ordenados por urgência.")]
    public static string GetUnreadEmails()
    {
        var urgentes  = Emails.Where(e => e.Urgente).ToList();
        var normais   = Emails.Where(e => !e.Urgente).ToList();

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"E-mails não lidos: {Emails.Count} total ({urgentes.Count} urgentes)\n");

        if (urgentes.Any())
        {
            sb.AppendLine("🔴 URGENTES:");
            foreach (var e in urgentes)
                sb.AppendLine($"  De: {e.NomeRemetente} ({e.Remetente})\n  Assunto: {e.Assunto}\n");
        }

        if (normais.Any())
        {
            sb.AppendLine("⚪ NORMAIS:");
            foreach (var e in normais)
                sb.AppendLine($"  De: {e.NomeRemetente} ({e.Remetente})\n  Assunto: {e.Assunto}\n");
        }

        return sb.ToString().TrimEnd();
    }

    [Description("Retorna o conteúdo completo de um e-mail pelo nome do remetente ou assunto.")]
    public static string GetEmailDetails(
        [Description("Nome do remetente ou parte do assunto para buscar")] string busca)
    {
        var email = Emails.FirstOrDefault(e =>
            e.NomeRemetente.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
            e.Assunto.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
            e.Remetente.Contains(busca, StringComparison.OrdinalIgnoreCase));

        if (email is null)
            return $"Nenhum e-mail encontrado para '{busca}'.";

        return $"De:      {email.NomeRemetente} <{email.Remetente}>\n" +
               $"Assunto: {email.Assunto}\n" +
               $"Urgente: {(email.Urgente ? "Sim" : "Não")}\n\n" +
               $"{email.Corpo}";
    }

    // ── RECORDS / MODELOS ────────────────────────────────────

    private record Task(
        string Id, 
        string Titulo, 
        string Status,
        string Prioridade,
        string Responsavel,
        string Sprint)
    {
        public string Status { get; set; } = Status;
    }

    private record Meeting(
        string Titulo,
        string Inicio, 
        string Fim, 
        IEnumerable<string> Participantes);

    private record Email(
        string Remetente, 
        string NomeRemetente,
        string Assunto, 
        string Corpo, 
        bool Urgente);
}