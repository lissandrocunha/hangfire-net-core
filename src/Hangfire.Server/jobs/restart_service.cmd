@echo on
for /F "tokens=1-3 delims=:." %%a in ("%time%") do (
   set Hour=%%a
   set Minute=%%b
   set Seconds=%%c
)
::Convert HH:MM to minutes + 10
set /A newTime=Hour*60 + Minute + 10
rem Convert new time back to HH:MM
set /A Hour=newTime/60, Minute=newTime%%60

::rem Adjust new hour and minute
if %Hour% gtr 23 (set Hour=0) ELSE (IF %Hour% lss 10 set Hour=0%Hour%)
if %Minute% lss 10 set Minute=0%Minute%
Set TaskTime=%Hour%:%Minute%
Echo %TaskTime%

schtasks /delete /tn "Restart Hangfire" /f
schtasks /create /tn "Restart Hangfire" /tr "C:\Hangfire\jobs\task_restart_hangfire.cmd" /sc once /st %TaskTime% /ru "System"
schtasks /run /tn "Restart Hangfire" /i
