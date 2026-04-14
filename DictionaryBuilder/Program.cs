using DictionaryBuilder.Builders;

var inputFile = "./DictionaryBuilder/Files/srWaC1.1.01.xml";
var workFile = "./DictionaryBuilder/Files/Parsed/work.bin";
var finalFile = "./DictionaryBuilder/Files/Parsed/trigram_final.bin";

var builder = new TrigramDictionaryBuilder();
builder.Build(inputFile, workFile, finalFile);
Console.WriteLine("Trigram dictionary.json uspešno generisan ✅");