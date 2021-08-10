:stop
sc stop Hangfire

rem cause a ~10 second sleep before checking the service state
ping 127.0.0.1 -n 10 -w 1000 > nul

sc query Hangfire | find /I "ESTADO" | find "STOPPED"
if errorlevel 1 goto :stop
goto :start

:start
net start | find /I Hangfire > nul && goto :start
sc start Hangfire

rem cause a ~10 second sleep before checking the service state
ping 127.0.0.1 -n 10 -w 1000 > nul

sc query Hangfire | find /I "ESTADO" | find "RUNNING"
if errorlevel 1 goto :start
goto :restartOk

:restartOk
schtasks /delete /tn "Restart Hangfire" /f