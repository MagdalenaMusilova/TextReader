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

namespace TextReader;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void LoadButton_Click(object sender, RoutedEventArgs e)
    {
        string[] lines = ["One", "Two", "AAAA afg  eh he e e  a"];
        Reader.AppendLines(lines);
    }
    
    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        
    }
    
    private void SearchButton_Click(object sender, RoutedEventArgs e)
    {

    }
}