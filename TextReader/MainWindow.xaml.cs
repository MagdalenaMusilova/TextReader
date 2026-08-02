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

    }
    
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        
    }
    
    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        Reader.ShowSearchBox();
    }
}