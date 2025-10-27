Rem submit4.bat
Rem Copyright (c) 2025 Ishan Pranav
Rem Licensed under the MIT license.

ChDir ..\src\NaturalLanguageProcessing.InformationRetrieval

dotnet run

MkDir ..\submission
Copy Program.cs ..\submission\Program.cs
Copy output.txt ..\submission\output.txt
ChDir ..\..\test
