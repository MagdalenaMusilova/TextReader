using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using TextReader.TextInputs;

namespace TextReader;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ITextInput _textInput;

    public MainWindow()
    {
        InitializeComponent();
        _textInput = new FileTextInput("../../../Data/book.txt");
        Reader.Load(_textInput);

        PreviewKeyDown += MainWindow_PreviewKeyDown;
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SaveFileProcess();
            e.Handled = true;
        }
    }

    private void LoadFileProcess()
    {
        var dialog = new LoadOptionsDialog { Owner = this };
        if (dialog.ShowDialog() == true && dialog.SelectedTextInput != null)
        {
            _textInput = dialog.SelectedTextInput;
            Reader.Load(_textInput);
        }
    }

    private async Task SaveFileProcess()
    {
        var saveFileDialog = new SaveFileDialog
        {
            Title = "Save Text To File",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            DefaultExt = ".txt"
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            try
            {
                await _textInput.SaveToFileAsync(saveFileDialog.FileName);
                await ShowSuccessNotification("File saved successfully!");
            }
            catch (Exception ex)
            {
                await ShowErrorNotification($"Error saving file: {ex.Message}");
            }
        }
    }

    private async Task ShowErrorNotification(string message)
    {
        ErrorNotificationText.Text = message;
        ErrorNotification.Visibility = Visibility.Visible;
        await Task.Delay(3000);
        ErrorNotification.Visibility = Visibility.Collapsed;
    }

    private async Task ShowSuccessNotification(string message)
    {
        ErrorNotificationText.Text = message;
        ErrorNotification.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 125, 50)); // Green
        ErrorNotification.Visibility = Visibility.Visible;
        await Task.Delay(2000);
        ErrorNotification.Visibility = Visibility.Collapsed;
        ErrorNotification.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(211, 47, 47)); // Reset to red
    }
    
    // Buttons
    
    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        LoadFileProcess();
    }
    
    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        await SaveFileProcess();
    }
    
    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        Reader.ToggleSearchBox();
    }
}