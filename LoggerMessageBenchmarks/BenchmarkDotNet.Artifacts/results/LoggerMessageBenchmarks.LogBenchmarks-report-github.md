```

BenchmarkDotNet v0.14.0, macOS 26.2 (25C56) [Darwin 25.2.0]
Apple M4, 1 CPU, 10 logical and 10 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.25.52411), Arm64 RyuJIT AdvSIMD
  DefaultJob : .NET 10.0.0 (10.0.25.52411), Arm64 RyuJIT AdvSIMD


```
| Method                      | Categories | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------- |----------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| &#39;interpolated $&quot;...&quot;&#39;       | Disabled   | 29.3315 ns | 0.2128 ns | 0.1991 ns |     ? |       ? | 0.0153 |     128 B |           ? |
| &#39;templated &quot;{X}&quot;, args&#39;     | Disabled   | 15.6664 ns | 0.1627 ns | 0.1359 ns |     ? |       ? | 0.0115 |      96 B |           ? |
| LoggerMessage.Define        | Disabled   |  0.4941 ns | 0.0026 ns | 0.0024 ns |     ? |       ? |      - |         - |           ? |
| &#39;[LoggerMessage] sourcegen&#39; | Disabled   |  0.0000 ns | 0.0000 ns | 0.0000 ns |     ? |       ? |      - |         - |           ? |
|                             |            |            |           |           |       |         |        |           |             |
| &#39;interpolated $&quot;...&quot;&#39;       | Enabled    | 28.9668 ns | 0.0378 ns | 0.0335 ns |  1.00 |    0.00 | 0.0153 |     128 B |        1.00 |
| &#39;templated &quot;{X}&quot;, args&#39;     | Enabled    | 77.8853 ns | 1.1862 ns | 1.1095 ns |  2.69 |    0.04 | 0.0267 |     224 B |        1.75 |
| LoggerMessage.Define        | Enabled    | 39.5834 ns | 0.0631 ns | 0.0527 ns |  1.37 |    0.00 | 0.0153 |     128 B |        1.00 |
| &#39;[LoggerMessage] sourcegen&#39; | Enabled    | 39.3799 ns | 0.0462 ns | 0.0386 ns |  1.36 |    0.00 | 0.0153 |     128 B |        1.00 |
