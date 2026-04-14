using System.Text;

namespace API.Models
{
    public class TrigramModel
    {
        private readonly Dictionary<string, List<string>> _trigrams;

        public TrigramModel(string binPath)
        {
            _trigrams = LoadBinary(binPath);
            Console.WriteLine($"📚 Trigram učitan: {_trigrams.Count} ključeva");
        }

        public List<string> PredictNext(string prevTwoWords)
        {
            prevTwoWords = prevTwoWords.ToLowerInvariant().Trim();

            return _trigrams.TryGetValue(prevTwoWords, out var next)
                ? next
                : new List<string>();
        }

        // ================= BIN LOADER =================

        private static Dictionary<string, List<string>> LoadBinary(string path)
        {
            var result = new Dictionary<string, List<string>>();

            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs, Encoding.UTF8);

            int keyCount = br.ReadInt32();

            for (int i = 0; i < keyCount; i++)
            {
                var key = br.ReadString();
                int valueCount = br.ReadInt32();

                var list = new List<string>(valueCount);
                for (int j = 0; j < valueCount; j++)
                    list.Add(br.ReadString());

                result[key] = list;
            }

            return result;
        }
    }
}