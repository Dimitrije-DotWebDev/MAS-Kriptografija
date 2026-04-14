using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace DictionaryBuilder.Builders
{
    public class TrigramDictionaryBuilder
    {
        private const int CHUNK_SIZE = 500_000;

        private readonly HashSet<string> _stopWords = new()
        {
            "i","a","ali","da","se","je","sam","si","smo","ste","su","će",
            "bi","bih","bismo","biste","budu","ne","ni","na","u","o","od",
            "po","za","kao","kod","bez","do","sa","između","kroz","preko",
            "oko","među","ili","što","šta","ko","koji","čiji","čija","čije",
            "kad","dok","jer","pa","još","već","tada","onda"
        };

        // =====================================================
        // PUBLIC ENTRY
        // =====================================================

        public void Build(
            string inputPath,
            string workBinPath,
            string finalBinPath,
            int topN = 3)
        {
            var chunk = new Dictionary<string, Dictionary<string, int>>();
            string? prev1 = null;
            string? prev2 = null;

            long totalBytes = new FileInfo(inputPath).Length;
            var stopwatch = Stopwatch.StartNew();

            using var fs = File.OpenRead(inputPath);
            using var reader = new StreamReader(fs, Encoding.UTF8);

            int linesInChunk = 0;
            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                var word = ExtractWord(line);

                if (word != null && prev1 != null && prev2 != null)
                {
                    var key = $"{prev2} {prev1}";

                    if (!chunk.TryGetValue(key, out var nexts))
                        chunk[key] = nexts = new Dictionary<string, int>();

                    nexts[word] = nexts.GetValueOrDefault(word) + 1;
                }

                prev2 = prev1;
                prev1 = word;

                if (++linesInChunk >= CHUNK_SIZE)
                {
                    MergeChunkBinary(chunk, workBinPath);
                    chunk.Clear();
                    linesInChunk = 0;

                    PrintProgress(fs.Position, totalBytes, stopwatch);
                }
            }

            if (chunk.Count > 0)
                MergeChunkBinary(chunk, workBinPath);

            Console.WriteLine("📦 Kreiram FINALNI binarni trigram fajl...");
            BuildFinalBinary(workBinPath, finalBinPath, topN);

            Console.WriteLine("✅ Trigram rečnik uspešno napravljen");
        }

        // =====================================================
        // TEXT PARSING
        // =====================================================

        private string? ExtractWord(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            var parts = line.Split('\t');
            if (parts.Length == 0)
                return null;

            var word = parts[0].ToLowerInvariant();

            if (_stopWords.Contains(word))
                return null;

            if (word.Length < 2 || !word.All(char.IsLetter))
                return null;

            return word;
        }

        // =====================================================
        // WORK BIN (MERGE)
        // =====================================================

        private static void MergeChunkBinary(
            Dictionary<string, Dictionary<string, int>> chunk,
            string path)
        {
            var work = LoadBinary(path);

            foreach (var (key, values) in chunk)
            {
                if (!work.TryGetValue(key, out var nexts))
                    work[key] = nexts = new Dictionary<string, int>();

                foreach (var (word, count) in values)
                    nexts[word] = nexts.GetValueOrDefault(word) + count;
            }

            SaveBinary(work, path);
        }

        private static void SaveBinary(
            Dictionary<string, Dictionary<string, int>> data,
            string path)
        {
            using var fs = File.Create(path);
            using var bw = new BinaryWriter(fs, Encoding.UTF8);

            bw.Write(data.Count);

            foreach (var (key, nexts) in data)
            {
                bw.Write(key);
                bw.Write(nexts.Count);

                foreach (var (word, count) in nexts)
                {
                    bw.Write(word);
                    bw.Write(count);
                }
            }
        }

        private static Dictionary<string, Dictionary<string, int>> LoadBinary(string path)
        {
            if (!File.Exists(path))
                return new Dictionary<string, Dictionary<string, int>>();

            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs, Encoding.UTF8);

            var result = new Dictionary<string, Dictionary<string, int>>();
            int keyCount = br.ReadInt32();

            for (int i = 0; i < keyCount; i++)
            {
                var key = br.ReadString();
                int valueCount = br.ReadInt32();

                var nexts = new Dictionary<string, int>();
                for (int j = 0; j < valueCount; j++)
                {
                    var word = br.ReadString();
                    var count = br.ReadInt32();
                    nexts[word] = count;
                }

                result[key] = nexts;
            }

            return result;
        }

        // =====================================================
        // FINAL BIN
        // =====================================================

        private static void BuildFinalBinary(
            string workPath,
            string finalPath,
            int topN)
        {
            var work = LoadBinary(workPath);

            using var fs = File.Create(finalPath);
            using var bw = new BinaryWriter(fs, Encoding.UTF8);

            bw.Write(work.Count);

            foreach (var (key, nexts) in work)
            {
                var top = nexts
                    .OrderByDescending(x => x.Value)
                    .Take(topN)
                    .Select(x => x.Key)
                    .ToList();

                bw.Write(key);
                bw.Write(top.Count);

                foreach (var word in top)
                    bw.Write(word);
            }
        }

        // =====================================================
        // PROGRESS
        // =====================================================

        private static void PrintProgress(
            long processed,
            long total,
            Stopwatch sw)
        {
            double percent = processed * 100.0 / total;
            double speed = processed / sw.Elapsed.TotalSeconds;
            double remainingSeconds = (total - processed) / speed;

            Console.WriteLine(
                $"[{percent:0.00}%] " +
                $"Elapsed: {sw.Elapsed:hh\\:mm\\:ss} | " +
                $"ETA: {TimeSpan.FromSeconds(remainingSeconds):hh\\:mm\\:ss}"
            );
        }
    }
}
