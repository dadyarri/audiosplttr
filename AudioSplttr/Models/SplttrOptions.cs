namespace AudioSplttr.Models;

public class SplttrOptions
{
    public string InputFile { get; set; }
    public string OutputPath { get; set; }
    public int PauseSize { get; set; }
    public int SilenceVolume { get; set; }
    public string NameTemplate { get; set; }
    public string FFmpegPath { get; set; }
    public SplttrAudioFormats OutputFormat { get; set; }
    public bool Rewrite { get; set; }
}