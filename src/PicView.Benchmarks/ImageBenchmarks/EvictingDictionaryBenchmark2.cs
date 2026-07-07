using BenchmarkDotNet.Attributes;
using ImageMagick;
using PicView.Benchmarks.Resources;
using PicView.Core.FileHandling;
using PicView.Core.ImageReading;
using ZLinq;

namespace PicView.Benchmarks.ImageBenchmarks;

[MemoryDiagnoser] // track allocations
public class EvictingDictionaryBenchmark2
{
    private List<FileInfo>? _fileInfos;
    private const int MaxSize = 12;
    private Core.Preloading.EvictingDictionary2<MagickImage> _hybridEvictingDict = new(MaxSize);
    private EvictingDictionary2Span<MagickImage> _spanEvictingDict = new(MaxSize);
    private EvictingDictionary2Legacy<MagickImage> _legacyEvictingDictOld = new(MaxSize);
    private List<MagickImage> _images;

    [GlobalSetup]
    public async Task Setup()
    {
        var picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        _fileInfos = new DirectoryInfo(picturesPath)
            .DescendantsAndSelf()
            .OfType<FileInfo>()
            .Where(x => x.IsSupported())
            .Take(MaxSize * 3)
            .ToList();
        _images = [];
        await Parallel.ForEachAsync(_fileInfos, async (file, _) =>
        {
            try
            {
                _images.Add(await MagickPerformanceReader.ReadMagickImageWithSpanAsync(file));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Skipping {file.Name}: {ex.Message}");
            }
        });
    }

    [Benchmark]
    public void TestImagesHybrid()
    {
        for (var i = 0; i < _images.Count; i++)
        {
            _hybridEvictingDict.TryAdd(Guid.NewGuid().ToString("N"), i, _fileInfos[i].FullName, _images[i], _images.Count,
                false, out _);
        }

        for (var i = 0; i < _images.Count; i++)
        {
            _hybridEvictingDict.TryGetValueByPath(_fileInfos[i].FullName, out _);
        }
    }

    [Benchmark]
    public void TestImagesSpanTest()
    {
        for (var i = 0; i < _images.Count; i++)
        {
            _spanEvictingDict.TryAdd(Guid.NewGuid().ToString("N"), i, _fileInfos[i].FullName, _images[i], _images.Count,
                false, out _);
        }

        for (var i = 0; i < _images.Count; i++)
        {
            _spanEvictingDict.TryGetValueByPath(_fileInfos[i].FullName, out _);
        }
    }

    [Benchmark]
    public void TestImagesLegacy()
    {
        for (var i = 0; i < _images.Count; i++)
        {
            _legacyEvictingDictOld.TryAdd(Guid.NewGuid().ToString("N"), i, _fileInfos[i].FullName, _images[i], _images.Count,
                false, out _);
        }

        for (var i = 0; i < _images.Count; i++)
        {
            _legacyEvictingDictOld.TryGetValueByPath(_fileInfos[i].FullName, out _);
        }
    }
}
/*

    // * Summary *
                                                                                                                                                                                                              
   BenchmarkDotNet v0.15.8, macOS Sequoia 15.6.1 (24G90) [Darwin 24.6.0]
   Apple M1, 1 CPU, 8 logical and 8 physical cores                                                                                                                                                            
   .NET SDK 10.0.100                                                                                                                                                                                          
     [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a                                                                                                                                 
     DefaultJob : .NET 10.0.0 (10.0.0, 10.0.25.52411), Arm64 RyuJIT armv8.0-a                                                                                                                                 
                                                                                                                                                                                                              
   
   | Method             | Mean     | Error    | StdDev   | Gen0   | Allocated |
   |------------------- |---------:|---------:|---------:|-------:|----------:|
   | TestImagesHybrid   | 22.76 us | 0.077 us | 0.064 us | 0.4883 |   3.09 KB |                                                                                                                               
   | TestImagesSpanTest | 31.80 us | 0.023 us | 0.020 us | 2.7466 |  16.88 KB |
   | TestImagesLegacy   | 26.39 us | 0.086 us | 0.072 us | 1.8616 |  11.48 KB |

*/
