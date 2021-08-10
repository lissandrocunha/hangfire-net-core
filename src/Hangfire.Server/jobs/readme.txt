Pasta onde serão armazenados os jobs executados no hangfire

OBS: Arquivo de configurações do Job deve possuir o nome "jobsettings.json" e obrigatóriamente deve conter as informações:

{
  "Assembly": {
    "Nome": "jobName",  => nome do job onde será armazenado no Hangfire, sem espações ou acentuação (ex: [a-z][0-9]).
    "LogLevel": "Debug", => nível de informação que será considerado nos logs.
    "DLL": "Hangfire.Job.Cotacao.dll", => nome da dll que implementa a classe HangfireJob.
    "CRON": " */10 23-23 * * 1-5 ", => informação de execução de Jobs do tipo RecurringJob (true).
    "Queue": "financeiro", => fila que o job faz parte.
    "LogMethod": [ "sentry", "file" ], => tipos de método de log (sentry ou file). Se for do tipo Sentry deverá conter as informações da TAG "Sentry" (exemplo abaixo)
    "RecurringJob": true => True para que o Job seja executado contínuamente levando em consideração as configurações de CRON.
  },
  "Sentry": {
    "Dsn": "", => url do projeto Sentry, obrigatório!
    "Release": "v 1.0.0", => campo opcional
    "Environment": "Development", => campo opcional
	"LogLevel": "Error" => campo opcional, se não for informado será levado em consideração o LogLevel informado em "Assembly"
  }
  ...
}



CRON( mm hh DD MM DoW): 
 
 Sintaxe da crontab:
*   *   *   *   *       caminho/comando
│   │   │   │   │
│   │   │   │   └────── em quais dias da semana de 0 a 7 (tanto 0 quanto 7 são Domingo)
│   │   │   └────────── em quais meses    (1 - 12)
│   │   └────────────── em quais dias     (1 - 31)
│   └────────────────── em quais horas    (0 - 23)
└────────────────────── em quais minutos  (0 - 59)

Especificando cada ítem:
*        Todos
1,2,4    Um, dois e Quatro apenas
7-10     De 7 a 10, incluindo 8 e 9
*/5      A qualquer momento, mas com espaço de 5 (ex: 2,7,12,17...)
1-10/3   No intervalo de 1 a 10, mas de 3 em 3 (ex: 2,5,8)
Nota: quando você usa /n, depende do momento em que o cron é atualizado pela tabela que o intervalo é contado, portanto, */5 pode ser tanto 0,5,10,15 quanto 1,6,11,16.

Site para verificar regras do CRONTAB: https://crontab.guru/