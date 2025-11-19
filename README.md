# [sunnyxxy的osu!mania StarRating算法](https://github.com/sunnyxxy/Star-Rating-Rebirth) C#版

## 使用

### 通过库

安装NuGet包：
```bash
dotnet add package StarRatingRebirth
```
示例代码：
```csharp
var data = ManiaData.FromFile(osu_file_path);
data = data.HT(); // if you want to use HT mod
var sr = SRCalculator.Calculate(data);
```

### 通过命令行工具

从[Releases](https://github.com/zzzzv/StarRatingRebirth/releases)下载, 按照提示操作。