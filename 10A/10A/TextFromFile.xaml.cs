using System.Windows;

namespace _10A
{
    public partial class TextFromFile : Window
    {
        public TextFromFile()
        {
            InitializeComponent();
        }

        public string Text { get; set; }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textBlock.Text = Text;
        }
    }
}