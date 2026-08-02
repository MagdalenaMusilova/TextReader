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
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new LoadOptionsDialog { Owner = this };
        if (dialog.ShowDialog() == true && dialog.SelectedTextInput != null)
        {
            _textInput = dialog.SelectedTextInput;
            Reader.Load(_textInput);
        }
    }
    
    private async void SaveButton_Click(object sender, RoutedEventArgs e)
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
                MessageBox.Show("File saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
    
    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        Reader.ShowSearchBox();
    }
}