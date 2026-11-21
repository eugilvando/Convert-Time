namespace FileConverter.Views
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using CommunityToolkit.Mvvm.DependencyInjection;
    using FileConverter.Services;
    using Microsoft.Win32;

    public partial class ConvertWindow : Window
    {
        private readonly List<string> files = new List<string>();
        private readonly ISettingsService settings = Ioc.Default.GetRequiredService<ISettingsService>();
        private readonly Action<List<string>, ConversionPreset> convert;
        private ConversionPreset batch;
        private string folder;

        public ConvertWindow()
        {
            var app = (FileConverter.Application)System.Windows.Application.Current;
            this.convert = app.StartBatch;
            Closed += (sender, args) => Ioc.Default.GetRequiredService<INavigationService>().Close(Pages.Convert, true);
            InitializeComponent();
            Themes.SelectedIndex = ConvertTheme.Mode;
            Categories.ItemsSource = new[] { "Todos", "Imagem", "Áudio", "Vídeo", "Documentos" };
            Categories.SelectedIndex = 0;
            Add(app.InitialFiles);
        }

        private static string Category(OutputType type)
        {
            switch (type)
            {
                case OutputType.Aac: case OutputType.Flac: case OutputType.Mp3: case OutputType.Ogg: case OutputType.Wav: return "Áudio";
                case OutputType.Avi: case OutputType.Mkv: case OutputType.Mp4: case OutputType.Ogv: case OutputType.Webm: return "Vídeo";
                case OutputType.Pdf: return "Documentos";
                default: return "Imagem";
            }
        }

        private void Add(IEnumerable<string> paths)
        {
            foreach (string path in paths.Where(File.Exists))
            {
                if (!files.Contains(path, StringComparer.OrdinalIgnoreCase)) files.Add(path);
            }
            FileList.ItemsSource = files.ToArray();
            RefreshProfiles();
        }

        private void RefreshProfiles()
        {
            var previous = Presets.SelectedItem as ConversionPreset;
            var category = Categories.SelectedItem as string;
            var extensions = files.Select(path => Path.GetExtension(path).TrimStart('.')).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            var compatible = settings.Settings?.ConversionPresets.Where(p =>
                (category == "Todos" || Category(p.OutputType) == category) &&
                extensions.All(ext => p.InputTypes.Contains(ext, StringComparer.OrdinalIgnoreCase))).ToList() ?? new List<ConversionPreset>();
            var temporary = batch;
            Presets.ItemsSource = compatible;
            Presets.SelectedItem = compatible.Contains(previous) ? previous : compatible.FirstOrDefault();
            if (temporary != null && previous != null && previous == Presets.SelectedItem) SetBatch(temporary);
            ConvertButton.Content = files.Count == 1 ? "Converter 1 ficheiro" : $"Converter {files.Count} ficheiros";
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            ConvertButton.IsEnabled = files.Count > 0 && batch != null;
            StatusText.Text = files.Count == 0 ? "Adicione ou arraste os ficheiros para começar." :
                batch == null ? "Nenhum perfil nesta categoria aceita todos os ficheiros selecionados." : $"{files.Count} ficheiro(s) no lote";
        }

        private void SetBatch(ConversionPreset preset)
        {
            batch = preset;
            Options.DataContext = preset;
            Options.Content = preset;
        }

        private void PresetChanged(object sender, SelectionChangedEventArgs e)
        {
            var preset = Presets.SelectedItem as ConversionPreset;
            SetBatch(preset == null ? null : new ConversionPreset(preset.ShortName, preset) { InputPostConversionAction = preset.InputPostConversionAction });
            UpdateStatus();
        }

        private void CategoryChanged(object sender, SelectionChangedEventArgs e) => RefreshProfiles();
        private void ThemeSelected(object sender, SelectionChangedEventArgs e) => ConvertTheme.SetMode(Themes.SelectedIndex);
        private void AddFiles(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Multiselect = true, Title = "Adicionar ficheiros" };
            if (dialog.ShowDialog(this) == true) Add(dialog.FileNames);
        }
        private void RemoveFiles(object sender, RoutedEventArgs e)
        {
            foreach (string file in FileList.SelectedItems.Cast<string>().ToArray()) files.Remove(file);
            Add(new string[0]);
        }
        private void FilesDropped(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] paths) Add(paths);
        }
        private void BrowseFolder(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) Destination.Text = folder = dialog.SelectedPath;
            }
        }
        private void ResetFolder(object sender, RoutedEventArgs e) { folder = null; Destination.Text = "Definido pelo perfil"; }
        private void OpenProfiles(object sender, RoutedEventArgs e)
        {
            var navigation = Ioc.Default.GetRequiredService<INavigationService>();
            navigation.Show(Pages.Settings);
            var window = System.Windows.Application.Current.Windows.OfType<SettingsWindow>().FirstOrDefault();
            if (window != null) { window.Closed -= ProfilesClosed; window.Closed += ProfilesClosed; }
        }
        private void ProfilesClosed(object sender, EventArgs e) => RefreshProfiles();
        private void ConvertClick(object sender, RoutedEventArgs e)
        {
            if (batch == null || files.Count == 0) return;
            if (files.Any(path => !File.Exists(path))) { StatusText.Text = "Um ficheiro deixou de estar disponível. Remova-o do lote."; return; }
            if (batch.InputPostConversionAction != InputPostConversionAction.None)
            {
                string action = batch.InputPostConversionAction == InputPostConversionAction.Delete ? "elimina" : "move";
                if (MessageBox.Show(this, $"Este perfil {action} os originais após a conversão. Continuar?", "Convert Time", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
            }
            if (folder != null) batch.OutputFileNameTemplate = Path.Combine(folder, "(f)");
            try { convert(files, batch); Close(); }
            catch (Exception error) { StatusText.Text = error.Message; }
        }
    }
}