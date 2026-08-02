using System.Windows;
using Microsoft.Win32;
using TextReader.TextInputs;

namespace TextReader;

public partial class LoadOptionsDialog : Window
{
    public ITextInput? SelectedTextInput { get; private set; }

    public LoadOptionsDialog()
    {
        InitializeComponent();
    }

    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
        // Return early if controls aren't initialized yet
        if (RandomTextPanel == null || FilePanel == null || WebPanel == null)
            return;

        // Hide all panels
        RandomTextPanel.Visibility = Visibility.Collapsed;
        FilePanel.Visibility = Visibility.Collapsed;
        WebPanel.Visibility = Visibility.Collapsed;

        // Show the selected panel
        if (RandomTextRadio?.IsChecked == true)
            RandomTextPanel.Visibility = Visibility.Visible;
        else if (FileRadio?.IsChecked == true)
            FilePanel.Visibility = Visibility.Visible;
        else if (WebRadio?.IsChecked == true)
            WebPanel.Visibility = Visibility.Visible;
    }

    private void LoadRandomText_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(RandomTextSizeInput.Text, out int size) && size > 0)
        {
            SelectedTextInput = new RandomTextInput(size);
            DialogResult = true;
            Close();
        }
        else
        {
            MessageBox.Show("Please enter a valid positive number for sentences.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void LoadFromFile_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Title = "Select Text File",
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            try
            {
                SelectedTextInput = new FileTextInput(openFileDialog.FileName);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void LoadFromWeb_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(WebUrlInput.Text))
        {
            MessageBox.Show("Please enter a URL.", "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            SelectedTextInput = new UrlTextInput(WebUrlInput.Text);
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading from web: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
