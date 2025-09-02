// File: Program.cs
// Target framework: net8.0
#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;

internal static class Program
{
    private record Resultado(string FileName, int Lines, int Words);

    public static async Task<int> Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("=== Processador de Arquivos de Texto (NET 8) ===\n");

        Console.Write("Informe o diretório onde estão os arquivos .txt: ");
        var dir = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
        {
            Console.WriteLine("Diretório inválido!");
            return 1;
        }

        // Busca arquivos .txt (apenas pasta atual; ajuste para AllDirectories se quiser)
        var arquivos = Directory.EnumerateFiles(dir, "*.txt", SearchOption.TopDirectoryOnly)
                                .OrderBy(Path.GetFileName)
                                .ToArray();

        if (arquivos.Length == 0)
        {
            Console.WriteLine("Nenhum arquivo .txt encontrado.");
            return 0;
        }

        Console.WriteLine("\nArquivos encontrados:");
        for (int i = 0; i < arquivos.Length; i++)
            Console.WriteLine($" {i + 1,2}. {Path.GetFileName(arquivos[i])}");

        Console.Write("\nSelecione arquivos (ex.: 1,3-5) ou Enter para todos: ");
        var selecao = Console.ReadLine()?.Trim();
        var arquivosSelecionados = ParseSelecao(selecao, arquivos);

        Console.WriteLine($"\nIniciando processamento de {arquivosSelecionados.Count} arquivo(s)...\n");

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => { e.Cancel = true; cts.Cancel(); };

        var resultados = new ConcurrentBag<Resultado>();
        var erros = new ConcurrentBag<string>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cts.Token
        };

        var start = DateTimeOffset.Now;

        try
        {
            await Parallel.ForEachAsync(arquivosSelecionados, options, async (caminho, ct) =>
            {
                var nome = Path.GetFileName(caminho);
                try
                {
                    Console.WriteLine($"Processando: {nome}");
                    var (linhas, palavras) = await ContarLinhasEPalavrasAsync(caminho, ct);
                    resultados.Add(new Resultado(nome, linhas, palavras));
                }
                catch (OperationCanceledException)
                {
                    erros.Add($"{nome}: operação cancelada.");
                }
                catch (Exception ex)
                {
                    erros.Add($"{nome}: {ex.Message}");
                }
            });
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\nCancelado pelo usuário (Ctrl+C).");
        }

        // Consolida e exporta
        var exportDir = Path.Combine(dir, "export");
        Directory.CreateDirectory(exportDir);
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var txtPath = Path.Combine(exportDir, $"relatorio_{stamp}.txt");
        var csvPath = Path.Combine(exportDir, $"relatorio_{stamp}.csv");

        var ordenados = resultados.OrderBy(r => r.FileName).ToList();
        var totalLinhas = ordenados.Sum(r => r.Lines);
        var totalPalavras = ordenados.Sum(r => r.Words);

        // TXT amigável
        var sb = new StringBuilder();
        sb.AppendLine("=== Relatório de Processamento ===");
        sb.AppendLine($"Diretório: {dir}");
        sb.AppendLine($"Arquivos processados: {ordenados.Count}");
        sb.AppendLine($"Início: {start:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Fim:    {DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        foreach (var r in ordenados)
            sb.AppendLine($"{r.FileName} - {r.Lines} linhas - {r.Words} palavras");
        sb.AppendLine();
        sb.AppendLine($"TOTAL: {totalLinhas} linhas | {totalPalavras} palavras");
        if (erros.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("Erros:");
            foreach (var e in erros) sb.AppendLine($" - {e}");
        }
        await File.WriteAllTextAsync(txtPath, sb.ToString(), Encoding.UTF8);

        // CSV ; separador
        var csv = new StringBuilder();
        csv.AppendLine("Arquivo;Linhas;Palavras");
        foreach (var r in ordenados)
            csv.AppendLine($"{EscapeCsv(r.FileName)};{r.Lines};{r.Words}");
        csv.AppendLine($"TOTAL;{totalLinhas};{totalPalavras}");
        if (erros.Count > 0)
        {
            csv.AppendLine();
            csv.AppendLine("Erros;");
            foreach (var e in erros) csv.AppendLine($"{EscapeCsv(e)};");
        }
        await File.WriteAllTextAsync(csvPath, csv.ToString(), Encoding.UTF8);

        Console.WriteLine($"\nProcessamento concluído.");
        Console.WriteLine($"Relatório TXT: {txtPath}");
        Console.WriteLine($"Relatório CSV: {csvPath}");
        return 0;
    }

    // Conta linhas e palavras lendo em streaming
    private static async Task<(int linhas, int palavras)> ContarLinhasEPalavrasAsync(string caminho, CancellationToken ct)
    {
        int linhas = 0, palavras = 0;

        // Detecta BOM automaticamente
        using var fs = File.OpenRead(caminho);
        using var sr = new StreamReader(fs, detectEncodingFromByteOrderMarks: true);

        while (true)
        {
            ct.ThrowIfCancellationRequested();
            var line = await sr.ReadLineAsync();
            if (line is null) break;
            linhas++;
            palavras += CountWords(line.AsSpan());
        }

        return (linhas, palavras);
    }

    // Scanner simples: conta transições de whitespace -> não-whitespace
    private static int CountWords(ReadOnlySpan<char> s)
    {
        bool inWord = false;
        int count = 0;

        foreach (var ch in s)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (inWord) { count++; inWord = false; }
            }
            else
            {
                inWord = true;
            }
        }
        if (inWord) count++;
        return count;
    }

    private static List<string> ParseSelecao(string? selecao, string[] arquivos)
    {
        if (string.IsNullOrWhiteSpace(selecao)) return arquivos.ToList();

        var set = new SortedSet<int>();
        foreach (var token in selecao.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (token.Contains('-', StringComparison.Ordinal))
            {
                var parts = token.Split('-', 2, StringSplitOptions.TrimEntries);
                if (parts.Length == 2 && int.TryParse(parts[0], out var a) && int.TryParse(parts[1], out var b))
                {
                    if (a > b) (a, b) = (b, a);
                    for (int i = a; i <= b; i++) TryAddIndex(i);
                }
                else
                {
                    Console.WriteLine($"Intervalo inválido ignorado: '{token}'");
                }
            }
            else
            {
                if (int.TryParse(token, out var idx)) TryAddIndex(idx);
                else Console.WriteLine($"Índice inválido ignorado: '{token}'");
            }
        }

        if (set.Count == 0)
        {
            Console.WriteLine("Nenhum índice válido fornecido. Processando todos.");
            return arquivos.ToList();
        }

        return set.Select(i => arquivos[i - 1]).ToList();

        void TryAddIndex(int idx)
        {
            if (idx >= 1 && idx <= arquivos.Length) set.Add(idx);
            else Console.WriteLine($"Fora do intervalo: {idx}");
        }
    }

    private static string EscapeCsv(string s)
    {
        // Para separador ';', basta envolver se tiver aspas/;/\n
        if (s.IndexOfAny(new[] { ';', '"', '\n', '\r' }) >= 0)
            return $"\"{s.Replace("\"", "\"\"")}\"";
        return s;
    }
}
