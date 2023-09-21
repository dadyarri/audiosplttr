using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using AudioSplttr.Models;
using AudioSplttr.Splitter;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace AudioSplttr;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Dispatcher.UnhandledException += OnDispatcherUnhandledException;
        PickerFormat.ItemsSource = Enum.GetValues(typeof(SplttrAudioFormats)).Cast<SplttrAudioFormats>();
    }

    private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
    {
        Regex regex = new Regex("[^0-9]+");
        e.Handled = regex.IsMatch(e.Text);
    }

    private void BtnOpenOriginalFile_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "WAV (*.wav)|*.wav|MP3 (*.mp3)|*.mp3|Все файлы (*.*)|*.*"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            TextBoxOriginalFilePath.Text = openFileDialog.FileName;
            TextBoxNameTemplate.Text = Path.GetFileNameWithoutExtension(openFileDialog.FileName);
        }
    }

    private void BtnChooseOutputPath_Click(object sender, RoutedEventArgs e)
    {
        CommonOpenFileDialog ofd = new() { IsFolderPicker = true };
        if (ofd.ShowDialog() == CommonFileDialogResult.Ok)
        {
            TextBoxOutputPath.Text = ofd.FileName;
        }
    }

    private async void ButtonStart_Click(object sender, RoutedEventArgs e)
    {
        var config = new SplttrOptions
        {
            InputFile = TextBoxOriginalFilePath.Text,
            OutputPath = TextBoxOutputPath.Text,
            PauseSize = int.Parse(TextBoxPauseLength.Text),
            SilenceVolume = int.Parse(TextBoxSilenceVolume.Text),
            NameTemplate = TextBoxNameTemplate.Text,
            FFmpegPath = TextBoxFFmpegPath.Text,
            OutputFormat = (SplttrAudioFormats)PickerFormat.SelectedItem,
            Rewrite = Rewrite.IsChecked ?? false
        };

        ProgressBarParent.Visibility = Visibility.Visible;

        await foreach (var progress in AudioSplitter.Split(config))
        {
            ProgressBar.Value = progress;
        }

        var userResponse = MessageBox.Show(
            "Завершено. Открыть папку с чанками?",
            "AudioSplttr",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information,
            MessageBoxResult.Yes);

        if (userResponse == MessageBoxResult.Yes)
        {
            var startInfo = new ProcessStartInfo
            {
                Arguments = config.OutputPath,
                FileName = "explorer.exe"
            };

            Process.Start(startInfo);
        }

        ProgressBarParent.Visibility = Visibility.Collapsed;
        ProgressBar.Value = 0;
    }

    private void BtnChooseFFmpegPath_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Исполняемый файл Windows (*.exe)|*.exe|Все файлы (*.*)|*.*"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            TextBoxFFmpegPath.Text = openFileDialog.FileName;
        }
    }

    private static void OnDispatcherUnhandledException(object sender,
        System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var errorMessage = $"An unhandled exception occurred: {e.Exception.Message}";
        MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        var config = new InjectorOptions
        {
            ChunksPath = TextBoxChunksPath.Text,
            FilePath = TextBoxExistingFilePath.Text,
        };

        await Injector.Inject(config);
        MessageBox.Show(
            "Завершено",
            "AudioSplttr",
            MessageBoxButton.OK,
            MessageBoxImage.Information,
            MessageBoxResult.Yes);
    }

    private void BtnChooseChunksPath_OnClick(object sender, RoutedEventArgs e)
    {
        CommonOpenFileDialog ofd = new() { IsFolderPicker = true };
        if (ofd.ShowDialog() == CommonFileDialogResult.Ok)
        {
            TextBoxChunksPath.Text = ofd.FileName;
        }
    }

    private void BtnChooseExistingFilePath_OnClick(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Скрипт Ren'py (*.rpy)|*.rpy|Все файлы (*.*)|*.*"
        };
        if (openFileDialog.ShowDialog() == true)
        {
            TextBoxExistingFilePath.Text = openFileDialog.FileName;
        }
    }
}