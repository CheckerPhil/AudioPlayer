using Gtk;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

class Program
{
    static Process mediaPlayer;
    static String musicFile = "test.mp3"; // Replace with the actual path to your music file

    static void Main(string[] args)
    {
        Application.Init();

        var window = new Window("Audio Player");
        window.SetDefaultSize(300, 200);
        window.DeleteEvent += (_, __) =>
        {
            if (mediaPlayer != null && !mediaPlayer.HasExited)
            {
                mediaPlayer.Kill();
                mediaPlayer.Dispose();
            }
            Application.Quit();
        };


        var button = new Button("Load Song");
        button.Clicked += (sender, e) =>
        {
            if (mediaPlayer == null || mediaPlayer.HasExited)
            {
                StartPlayback();
                button.Label = "Pause";
            }
            else
            {
                if (button.Label == "Pause")
                {
                    PausePlayback();
                    button.Label = "Play";
                }
                else
                {
                    ResumePlayback();
                    button.Label = "Pause";
                }
            }
        };

        var Filebutton = new Button("Choose File");
        Filebutton.Clicked += (sender, e) =>
        {
            var dialog = new FileChooserDialog(
                        "Select Music File",
                        window,
                        FileChooserAction.Open,
                        "Cancel", ResponseType.Cancel,
                        "Open", ResponseType.Accept);

                    if (dialog.Run() == (int)ResponseType.Accept)
                    {
                        musicFile = dialog.Filename;
                        File.Delete("temp.wav");
                        if (mediaPlayer != null && !mediaPlayer.HasExited)
                        {
                            mediaPlayer.Kill();
                            button.Label = "Play";
                        }
                    }

                    dialog.Dispose();
        };

        var box = new Box(Orientation.Vertical, 10);
        box.PackStart(Filebutton, false, false, 0);
        box.PackStart(button, false, false, 0);
        window.Add(box);

        window.ShowAll();

        Application.Run();
    }

    static void StartPlayback()
    {
        Console.WriteLine(musicFile);
        var tempFile = "temp.wav";

        if(!File.Exists(tempFile)){
            if(musicFile.EndsWith(".wav")){
                tempFile = musicFile;
            }else{
                // Convert MP3 to WAV using FFmpeg command-line tool
                var ffmpegProcess = new Process();
                ffmpegProcess.StartInfo.FileName = "ffmpeg";
                ffmpegProcess.StartInfo.Arguments = $"-i {musicFile} {tempFile}";
                ffmpegProcess.Start();
                ffmpegProcess.WaitForExit();
            }
        }

        mediaPlayer = new Process();
        mediaPlayer.StartInfo.FileName = "aplay";
        mediaPlayer.StartInfo.Arguments = tempFile;
        mediaPlayer.Start();
    }

    static void PausePlayback()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            SendSignalToProcess(mediaPlayer.Id, LinuxSignals.SIGSTOP);
        }
        else
        {
            mediaPlayer.Kill();
        }
    }

    static void ResumePlayback()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            SendSignalToProcess(mediaPlayer.Id, LinuxSignals.SIGCONT);
        }
        else
        {
            StartPlayback();
        }
    }

    static void SendSignalToProcess(int processId, LinuxSignals signal)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            if (process != null && !process.HasExited)
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "kill",
                    Arguments = $"-{signal.ToString().ToUpper()} {processId}",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(processStartInfo);
            }
        }
        catch (ArgumentException)
        {
            // Process with the specified ID not found
        }
    }

enum LinuxSignals
{
    SIGSTOP,
    SIGCONT
}
}