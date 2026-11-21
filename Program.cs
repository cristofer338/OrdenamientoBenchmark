using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace OrdenamientoBenchmark
{
    internal static class Program
    {
        private static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            int[] sizes = { 10000, 50000, 100000 };
            string[] scenarios = { "aleatorio", "ordenado", "inverso" };
            string csvPath = "resultados_benchmark.csv";

            // Por defecto omitimos Burbuja en tamaños mayores a 10,000 por su complejidad O(n^2)
            const int skipBubbleAbove = 10000;

            var rows = new List<string>
            {
                "algoritmo,tipo,tamano,escenario,tiempo_cpu_seg,valido"
            };

            foreach (int size in sizes)
            {
                foreach (string scenario in scenarios)
                {
                    int[] dataset = GenerateDataset(size, scenario, seed: 42);

                    var algorithms = new (string Name, string Type, Func<int[], int[]> SortFn)[]
                    {
                        ("Burbuja", "Interno", BubbleSort),
                        ("Quicksort", "Interno", Quicksort),
                        ("ShellSort", "Interno", ShellSort),
                        ("Radix", "Interno", RadixSort),
                        ("Intercalacion", "Externo", arr => ExternalIntercalacion(arr, chunkSize: 5000)),
                        ("Mezcla Directa", "Externo", arr => ExternalMezclaDirecta(arr, runSize: 2000)),
                        ("Mezcla Natural", "Externo", ExternalMezclaNatural)
                    };

                    foreach (var (name, type, sortFn) in algorithms)
                    {
                        if (name == "Burbuja" && size > skipBubbleAbove)
                        {
                            Console.WriteLine($"[SKIP] {name,-15} | {type,-7} | n={size,6} | {scenario,-9} | omitido por O(n^2)");
                            rows.Add($"{name},{type},{size},{scenario},,omitido");
                            continue;
                        }

                        TimeSpan cpuStart = Process.GetCurrentProcess().TotalProcessorTime;
                        int[] sorted = sortFn(dataset);
                        TimeSpan cpuEnd = Process.GetCurrentProcess().TotalProcessorTime;

                        double cpuSeconds = (cpuEnd - cpuStart).TotalSeconds;
                        bool isValid = IsSorted(sorted) && sorted.Length == dataset.Length;

                        Console.WriteLine(
                            $"{name,-15} | {type,-7} | n={size,6} | {scenario,-9} | CPU={cpuSeconds:F6}s | ok={isValid}"
                        );

                        rows.Add(string.Format(
                            CultureInfo.InvariantCulture,
                            "{0},{1},{2},{3},{4:F6},{5}",
                            name, type, size, scenario, cpuSeconds, isValid
                        ));
                    }
                }
            }

            File.WriteAllLines(csvPath, rows);
            Console.WriteLine($"\nResultados guardados en '{Path.GetFullPath(csvPath)}'.");
            Console.WriteLine("Presiona cualquier tecla para salir...");
            Console.ReadKey();
        }

        // ==========================================
        // 1. ALGORITMOS DE ORDENAMIENTO INTERNO
        // ==========================================

        private static int[] BubbleSort(int[] arr)
        {
            int[] a = (int[])arr.Clone();
            int n = a.Length;

            for (int i = 0; i < n; i++)
            {
                bool swapped = false;
                for (int j = 0; j < n - i - 1; j++)
                {
                    if (a[j] > a[j + 1])
                    {
                        (a[j], a[j + 1]) = (a[j + 1], a[j]);
                        swapped = true;
                    }
                }
                if (!swapped) break;
            }

            return a;
        }

        private static int[] Quicksort(int[] arr)
        {
            int[] a = (int[])arr.Clone();
            if (a.Length > 1)
            {
                QuicksortRange(a, 0, a.Length - 1);
            }
            return a;
        }

        private static void QuicksortRange(int[] a, int low, int high)
        {
            while (low < high)
            {
                int pivot = a[low + (high - low) / 2];
                int i = low;
                int j = high;

                while (i <= j)
                {
                    while (a[i] < pivot) i++;
                    while (a[j] > pivot) j--;
                    if (i <= j)
                    {
                        (a[i], a[j]) = (a[j], a[i]);
                        i++;
                        j--;
                    }
                }

                if (j - low < high - i)
                {
                    if (low < j) QuicksortRange(a, low, j);
                    low = i;
                }
                else
                {
                    if (i < high) QuicksortRange(a, i, high);
                    high = j;
                }
            }
        }

        private static int[] ShellSort(int[] arr)
        {
            int[] a = (int[])arr.Clone();
            int n = a.Length;
            int gap = 1;

            while (gap < n / 3)
            {
                gap = 3 * gap + 1;
            }

            while (gap > 0)
            {
                for (int i = gap; i < n; i++)
                {
                    int temp = a[i];
                    int j = i;
                    while (j >= gap && a[j - gap] > temp)
                    {
                        a[j] = a[j - gap];
                        j -= gap;
                    }
                    a[j] = temp;
                }
                gap /= 3;
            }

            return a;
        }

        private static int[] RadixSort(int[] arr)
        {
            if (arr.Length == 0) return Array.Empty<int>();
            int[] a = (int[])arr.Clone();
            const int Base = 10;

            int maxVal = a[0];
            for (int i = 1; i < a.Length; i++)
            {
                if (a[i] > maxVal) maxVal = a[i];
            }

            int n = a.Length;
            long exp = 1;

            while (maxVal / exp > 0)
            {
                int[] output = new int[n];
                int[] count = new int[Base];

                for (int i = 0; i < n; i++)
                {
                    int digit = (int)((a[i] / exp) % Base);
                    count[digit]++;
                }

                for (int i = 1; i < Base; i++)
                {
                    count[i] += count[i - 1];
                }

                for (int i = n - 1; i >= 0; i--)
                {
                    int digit = (int)((a[i] / exp) % Base);
                    output[count[digit] - 1] = a[i];
                    count[digit]--;
                }

                a = output;
                exp *= Base;
            }

            return a;
        }

        // ==========================================
        // 2. ALGORITMOS DE ORDENAMIENTO EXTERNO
        // ==========================================

        private static void WriteNumbers(string path, int[] nums, int start, int length)
        {
            using var writer = new StreamWriter(path);
            int end = Math.Min(start + length, nums.Length);
            for (int i = start; i < end; i++)
            {
                writer.WriteLine(nums[i]);
            }
        }

        private static int[] ReadNumbers(string path)
        {
            var list = new List<int>();
            using var reader = new StreamReader(path);
            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                if (int.TryParse(line, out int val))
                {
                    list.Add(val);
                }
            }
            return list.ToArray();
        }

        private static void MergeTwoFiles(string fileA, string fileB, string outPath)
        {
            using var ra = new StreamReader(fileA);
            using var rb = new StreamReader(fileB);
            using var fout = new StreamWriter(outPath);

            string? la = ra.ReadLine();
            string? lb = rb.ReadLine();
            int? va = la != null ? int.Parse(la) : null;
            int? vb = lb != null ? int.Parse(lb) : null;

            while (va.HasValue && vb.HasValue)
            {
                if (va.Value <= vb.Value)
                {
                    fout.WriteLine(va.Value);
                    la = ra.ReadLine();
                    va = la != null ? int.Parse(la) : null;
                }
                else
                {
                    fout.WriteLine(vb.Value);
                    lb = rb.ReadLine();
                    vb = lb != null ? int.Parse(lb) : null;
                }
            }

            while (va.HasValue)
            {
                fout.WriteLine(va.Value);
                la = ra.ReadLine();
                va = la != null ? int.Parse(la) : null;
            }

            while (vb.HasValue)
            {
                fout.WriteLine(vb.Value);
                lb = rb.ReadLine();
                vb = lb != null ? int.Parse(lb) : null;
            }
        }

        private static int[] ExternalIntercalacion(int[] arr, int chunkSize = 5000)
        {
            if (arr.Length == 0) return Array.Empty<int>();
            string tempDir = Path.Combine(Path.GetTempPath(), "ext_intercalacion_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var chunkFiles = new List<string>();
                for (int i = 0; i < arr.Length; i += chunkSize)
                {
                    int len = Math.Min(chunkSize, arr.Length - i);
                    int[] slice = new int[len];
                    Array.Copy(arr, i, slice, 0, len);
                    Array.Sort(slice);

                    string filePath = Path.Combine(tempDir, $"chunk_{i / chunkSize}.txt");
                    WriteNumbers(filePath, slice, 0, slice.Length);
                    chunkFiles.Add(filePath);
                }

                int pass = 0;
                while (chunkFiles.Count > 1)
                {
                    var nextFiles = new List<string>();
                    for (int i = 0; i < chunkFiles.Count; i += 2)
                    {
                        if (i + 1 < chunkFiles.Count)
                        {
                            string merged = Path.Combine(tempDir, $"p{pass}_m{i / 2}.txt");
                            MergeTwoFiles(chunkFiles[i], chunkFiles[i + 1], merged);
                            nextFiles.Add(merged);
                        }
                        else
                        {
                            nextFiles.Add(chunkFiles[i]);
                        }
                    }
                    chunkFiles = nextFiles;
                    pass++;
                }

                return ReadNumbers(chunkFiles[0]);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }

        private static int[] ExternalMezclaDirecta(int[] arr, int runSize = 2000)
        {
            if (arr.Length == 0) return Array.Empty<int>();
            string tempDir = Path.Combine(Path.GetTempPath(), "ext_directa_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var runs = new List<string>();
                for (int i = 0; i < arr.Length; i += runSize)
                {
                    int len = Math.Min(runSize, arr.Length - i);
                    int[] block = new int[len];
                    Array.Copy(arr, i, block, 0, len);
                    Array.Sort(block);

                    string runPath = Path.Combine(tempDir, $"run_{i / runSize}.txt");
                    WriteNumbers(runPath, block, 0, block.Length);
                    runs.Add(runPath);
                }

                int pass = 0;
                while (runs.Count > 1)
                {
                    var nextRuns = new List<string>();
                    for (int i = 0; i < runs.Count; i += 2)
                    {
                        if (i + 1 < runs.Count)
                        {
                            string outPath = Path.Combine(tempDir, $"d{pass}_m{i / 2}.txt");
                            MergeTwoFiles(runs[i], runs[i + 1], outPath);
                            nextRuns.Add(outPath);
                        }
                        else
                        {
                            nextRuns.Add(runs[i]);
                        }
                    }
                    runs = nextRuns;
                    pass++;
                }

                return ReadNumbers(runs[0]);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }

        private static int[] ExternalMezclaNatural(int[] arr)
        {
            if (arr.Length == 0) return Array.Empty<int>();
            string tempDir = Path.Combine(Path.GetTempPath(), "ext_natural_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);

            try
            {
                var runs = new List<string>();
                int n = arr.Length;
                int start = 0;
                int runIndex = 0;

                while (start < n)
                {
                    int end = start + 1;
                    while (end < n && arr[end - 1] <= arr[end])
                    {
                        end++;
                    }

                    string runPath = Path.Combine(tempDir, $"nat_{runIndex}.txt");
                    WriteNumbers(runPath, arr, start, end - start);
                    runs.Add(runPath);

                    start = end;
                    runIndex++;
                }

                int pass = 0;
                while (runs.Count > 1)
                {
                    var nextRuns = new List<string>();
                    for (int i = 0; i < runs.Count; i += 2)
                    {
                        if (i + 1 < runs.Count)
                        {
                            string outPath = Path.Combine(tempDir, $"n{pass}_m{i / 2}.txt");
                            MergeTwoFiles(runs[i], runs[i + 1], outPath);
                            nextRuns.Add(outPath);
                        }
                        else
                        {
                            nextRuns.Add(runs[i]);
                        }
                    }
                    runs = nextRuns;
                    pass++;
                }

                return ReadNumbers(runs[0]);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, recursive: true);
                }
            }
        }

        // ==========================================
        // 3. GENERADOR DE DATOS Y UTILIDADES
        // ==========================================

        private static int[] GenerateDataset(int size, string scenario, int seed)
        {
            var rng = new Random(seed);
            int[] data = new int[size];
            int maxRange = size * 10;

            for (int i = 0; i < size; i++)
            {
                data[i] = rng.Next(0, maxRange);
            }

            return scenario switch
            {
                "aleatorio" => data,
                "ordenado" => SortAscending(data),
                "inverso" => SortDescending(data),
                _ => throw new ArgumentException($"Escenario no válido: {scenario}")
            };
        }

        private static int[] SortAscending(int[] source)
        {
            int[] copy = (int[])source.Clone();
            Array.Sort(copy);
            return copy;
        }

        private static int[] SortDescending(int[] source)
        {
            int[] copy = (int[])source.Clone();
            Array.Sort(copy);
            Array.Reverse(copy);
            return copy;
        }

        private static bool IsSorted(int[] arr)
        {
            for (int i = 1; i < arr.Length; i++)
            {
                if (arr[i - 1] > arr[i]) return false;
            }
            return true;
        }
    }
}