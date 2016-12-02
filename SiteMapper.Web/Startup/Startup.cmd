if "%ComputeEmulatorRunning%"=="true" goto :EOF

REM Prevent the IIS app pools from shutting down due to being idle.
%windir%\system32\inetsrv\appcmd set config -section:applicationPools -applicationPoolDefaults.processModel.idleTimeout:00:00:00
 
REM Prevent IIS app pool recycles from recycling on the default schedule of 1740 minutes (29 hours).
%windir%\system32\inetsrv\appcmd set config -section:applicationPools -applicationPoolDefaults.recycling.periodicRestart.time:00:00:00
