using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UnitTests;
/// <summary>
/// MockBubbleWindow.xaml の相互作用ロジック
/// </summary>
public partial class MockBubbleWindow : Window
{
    public Person[] Persons { get; set; } = new Person[]
    {
        new() { Name = "AAAA" } ,
        new() { Name = "BBBB" } ,
        new() { Name = "CCCC" } ,
    };

    public MockBubbleWindow()
    {
        InitializeComponent();
    }
}

public class Person
{
    public string Name { get; set; } = "JANE DOE";

    public int Age { get; set; } = 30;
}
