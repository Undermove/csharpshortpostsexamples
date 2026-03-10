```

BenchmarkDotNet v0.14.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M4 2.40GHz, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.100
  [Host] : .NET 10.0.0 (10.0.25.52411), Arm64 RyuJIT AdvSIMD
  Dry    : .NET 10.0.0 (10.0.25.52411), Arm64 RyuJIT AdvSIMD

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method     | Mean      | Error | Ratio | Allocated | Alloc Ratio |
|----------- |----------:|------:|------:|----------:|------------:|
| &#39;1 строка&#39; |  8.685 ms |    NA |  1.00 |         - |          NA |
| &#39;10 строк&#39; | 11.946 ms |    NA |  1.38 |   27544 B |          NA |
