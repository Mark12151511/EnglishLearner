using CommunityToolkit.Mvvm.ComponentModel;

namespace EnglishLearner.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "EnglishLearner";
}
