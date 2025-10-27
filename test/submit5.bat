Rem submit5.bat
Rem Copyright (c) 2025 Ishan Pranav
Rem Licensed under the MIT license.

ChDir ..\src\NaturalLanguageProcessing.NounGroupTagger

dotnet run ..\..\data\WSJ_02-21.pos-chunk ..\..\test\training.feature
dotnet run ..\..\data\WSJ_24.pos-chunk ..\..\test\test.feature

ChDir ..\..\test

javac -cp maxent-3.0.0.jar;trove.jar *.java
java -cp .;maxent-3.0.0.jar;trove.jar MEtrain training.feature model.chunk
java -cp .;maxent-3.0.0.jar;trove.jar MEtag test.feature model.chunk response.chunk
python score.chunk.py WSJ_24.pos-chunk response.chunk

ChDir ..\src\NaturalLanguageProcessing.NounGroupTagger

dotnet run ..\..\data\WSJ_23.pos ..\..\test\test.feature

ChDir ..\..\test

java -cp .;maxent-3.0.0.jar;trove.jar MEtag test.feature model.chunk WSJ_23.chunk
