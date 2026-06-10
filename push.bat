@echo off
setlocal
set "PATH=C:\Program Files\Git\mingw64\bin;%PATH%"
set "GIT_SSH=C:\Program Files\TortoiseGit\bin\TortoiseGitPlink.exe"

tasklist /fi "imagename eq pageant.exe" | find /i "pageant.exe" >nul
if errorlevel 1 start "" "C:\Program Files\TortoiseGit\bin\pageant.exe" "%USERPROFILE%\.ssh\id_rsa.ppk"

git add -A
git commit -m "update"
git push
