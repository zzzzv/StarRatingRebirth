# sunnyxxy's osu!mania StarRating algorithm
## Usage
```
var data = ManiaData.FromFile(osu_file_path);
data = data.HT(); // if you want to use HT mod
var sr = SRCalculator.Calculate(data);
```