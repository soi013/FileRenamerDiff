namespace UnitTests;

public class InformationPageViewModel_Test
{
    [Fact]
    public void InformationPageViewModel_Create()
    {
        InformationPageViewModel.AppInfoText
            .Should().ContainAll("File Renamer Diff", "soi013");
    }
}
