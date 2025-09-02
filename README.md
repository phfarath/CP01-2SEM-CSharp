TextProcessor — Contador de Linhas e Palavras 

Aplicativo de console que permite selecionar arquivos .txt em uma pasta e processá-los de forma assíncrona e paralela, contando linhas e palavras de cada arquivo. Ao final, gera um relatório consolidado em TXT e CSV.

✨ Recursos

Leitura streaming (linha a linha) → baixo uso de memória
Contagem de palavras com qualquer whitespace (espaço, tab, quebras de linha)
Seleção de arquivos por índices: 1,3-5 ou Enter para todos
Execução paralela com limite automático (Environment.ProcessorCount)
Relatórios relatorio_YYYYMMDD_HHMMSS.txt e .csv na subpasta export
Log de erros por arquivo (sem interromper os demais)
Cancelamento por Ctrl + C


🧭 Uso passo a passo

Informe o diretório onde estão os .txt.
O app lista os arquivos encontrados com índices.
Digite os índices desejados (ex.: 2,5-7) ou pressione Enter para processar todos.
Acompanhe o progresso no console.
Ao finalizar, confira os relatórios na subpasta export do diretório selecionado.
Cancelar durante a execução: Ctrl + C.

📄 Formato dos relatórios
TXT (amigável)
=== Relatório de Processamento ===
Diretório: C:\Docs
Arquivos processados: 4
Início: 2025-09-02 20:15:10
Fim:    2025-09-02 20:15:11

clientes.txt - 120 linhas - 850 palavras
contratos.txt - 90 linhas - 615 palavras
...

TOTAL: 210 linhas | 1465 palavras

Erros:
 - notas.txt: Arquivo em uso por outro processo.

CSV (separador ;)
Arquivo;Linhas;Palavras
clientes.txt;120;850
contratos.txt;90;615
TOTAL;210;1465


📌 O separador é ; (pró-locale). Se preferir ,, altere facilmente no código.

🔧 Configuração & Personalização

Profundidade da busca: troque SearchOption.TopDirectoryOnly por AllDirectories se quiser incluir subpastas.
Grau de paralelismo: altere MaxDegreeOfParallelism se desejar limitar/aumentar threads.
Extensão filtrada: por padrão, *.txt. Mude o padrão para outros formatos.
Separador do CSV: ajuste a função que monta o CSV.
Regras de contagem: a função CountWords considera transições entre whitespace e não-whitespace. Adapte se precisar de regras específicas (ex.: hifens, línguas aglutinativas etc.).

🛡️ Tratamento de erros & encoding

Cada arquivo é processado com try/catch isolado → erros são listados no relatório, sem parar o lote.
StreamReader com detecção de BOM automática (UTF-8/UTF-16). Para encodings legados, troque o StreamReader por um com Encoding específico, se necessário.

🧠 Notas de implementação

A leitura linha a linha evita carregar o arquivo inteiro na memória, ideal para arquivos grandes.
A contagem de palavras usa um scanner (estado dentro/fora de “palavra”) com char.IsWhiteSpace.

🧪 Exemplo (sessão do console)
=== Processador de Arquivos de Texto (NET 8) ===

Informe o diretório onde estão os arquivos .txt: C:\Docs

Arquivos encontrados:
  1. clientes.txt
  2. contratos.txt
  3. notas.txt

Selecione arquivos (ex.: 1,3-5) ou Enter para todos: 1,3

Iniciando processamento de 2 arquivo(s)...

Processando: clientes.txt
Processando: notas.txt

Processamento concluído.
Relatório TXT: C:\Docs\export\relatorio_20250902_201511.txt
Relatório CSV: C:\Docs\export\relatorio_20250902_201511.csv

📦 Estrutura sugerida
TextProcessor/
  ├─ Program.cs
  └─ README.md

📋 Licença

Defina a licença de sua preferência (MIT, Apache-2.0, etc.).
