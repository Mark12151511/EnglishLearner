using System.Windows.Controls;

namespace EnglishLearner.App.Views;

public partial class HomeView : UserControl
{
    public event Action<string>? ModuleSelected;

    public HomeView()
    {
        InitializeComponent();
    }

    private void CardWordLearning_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ModuleSelected?.Invoke("WordLearning");
    }

    private void CardListening_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        ModuleSelected?.Invoke("ListeningSpeaking");
    }
}
