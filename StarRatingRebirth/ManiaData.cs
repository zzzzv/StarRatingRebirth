namespace StarRatingRebirth;

public struct Note(int key, int head, int tail)
{
    public int Key = key;
    public int Head = head;
    public int Tail = tail;
    public readonly bool IsLong => Tail != -1;

    public Note(int[] data) : this(data[0], data[1], data[2]) { }

    public readonly Note ChangeRate(double r)
    {
        return new Note(Key, (int)(Head * r), Tail == -1 ? -1 : (int)(Tail * r));
    }

    public override readonly bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }
        Note other = (Note)obj;
        return Key == other.Key && Head == other.Head && Tail == other.Tail;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Key, Head, Tail);
    }
}

public class ManiaData
{
    public List<Note> Notes { get; set; } = [];
    public int CS { get; set; }
    public double OD { get; set; }
    public ManiaData ChangeRate(double r)
    {
        var data = new ManiaData
        {
            Notes = Notes.Select(n => n.ChangeRate(r)).ToList(),
            CS = CS,
            OD = OD
        };
        return data;
    }
    public ManiaData DT()
    {
        return ChangeRate(2.0 / 3.0);
    }
    public ManiaData HT()
    {
        return ChangeRate(4.0 / 3.0);
    }

    public static ManiaData FromFile(string path)
    {
        string[] lines = File.ReadAllLines(path);
        return FromLines(lines);
    }

    /// <summary>
    /// Parse mania data from lines.
    /// </summary>
    /// <param name="lines">osu file lines</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException">Only mania 1-10K is supported.</exception>
    /// <exception cref="InvalidDataException">At least 20 notes are required.</exception>
    public static ManiaData FromLines(string[] lines)
    {
        string section = "";
        var data = new ManiaData();
        foreach (string line in lines)
        {
            string l = line.Trim();
            if (string.IsNullOrEmpty(l)) continue;
            if (l[0] == '[' && l[^1] == ']')
            {
                section = l[1..^1];
                continue;
            }
            switch (section)
            {
                case "General":
                case "Difficulty":
                    string[] parts = l.Split(':', 2);
                    if (parts.Length != 2) continue;
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    switch (key)
                    {
                        case "Mode":
                            if (value != "3") throw new NotSupportedException("Only mania mode is supported.");
                            break;
                        case "CircleSize":
                            data.CS = int.Parse(value);
                            if ( data.CS > 10) throw new NotSupportedException("10K+ is not supported.");
                            break;
                        case "OverallDifficulty":
                            data.OD = double.Parse(value);
                            break;
                    }
                    break;
                case "HitObjects":
                    string[] sp = l.Split(':', 2)[0].Split(',');
                    if (sp.Length < 4) continue;
                    data.Notes.Add(new Note(
                        (int)(int.Parse(sp[0]) * data.CS / 512.0),
                        int.Parse(sp[2]),
                        (int.Parse(sp[3]) & 128) != 0 ? int.Parse(sp[5]) : -1
                    ));
                    break;
            }
        }
        if (data.Notes.Count < 20)
        {
            throw new InvalidDataException($"Not enough notes: {data.Notes.Count}");
        }
        return data;
    }
}
