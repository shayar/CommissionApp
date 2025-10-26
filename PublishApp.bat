@echo off

SET PROJECT_NAME=EPC.WEB
SET INFRA_PROJECT=EPC.Infrastructure
SET PUBLISH_DIR=publish\EPC.App
SET RUNTIME=win-x64
SET CONFIG=Release

echo Cleaning...
dotnet clean %PROJECT_NAME%

echo Applying EF migrations to SQLite...
dotnet ef database update --project %INFRA_PROJECT% --startup-project %PROJECT_NAME%

echo Publishing app...
dotnet publish %PROJECT_NAME% -c %CONFIG% -r %RUNTIME% --self-contained true -o %PUBLISH_DIR% /p:PublishTrimmed=false

echo Copying SQLite DB...
copy %PROJECT_NAME%\epc_local.db %PUBLISH_DIR%

echo.
echo ? App published to: %PUBLISH_DIR%
pause
