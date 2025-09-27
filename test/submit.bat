Rem submit.bat
Rem Copyright (c) 2025 Ishan Pranav
Rem Licensed under the MIT license.

ChDir ..\src\NaturalLanguageProcessing.ViterbiTagger

dotnet run ..\..\data\WSJ_02-21.pos ..\..\data\WSJ_24.words
python3 ..\..\test\score.py ..\..\data\WSJ_24.pos submission.pos
dotnet run ..\..\data\training.pos ..\..\data\WSJ_23.words

MkDir ..\submission
Copy Program.cs ..\submission\ihp2012_HW3_Program.cs
Copy submission.pos ..\submission\submission.pos
Copy ..\..\README.md ..\submission\ihp2012_HW3_README.txt
ChDir
