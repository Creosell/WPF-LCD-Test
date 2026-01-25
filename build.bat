@echo off
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "WPF LCD Test.sln" /p:Configuration=Debug /t:Rebuild /v:minimal /nologo
