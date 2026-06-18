using VoiceType.Utils;

namespace VoiceType.Tests;

public class KeyCatalogRegressionTests
{
    [Fact]
    public void Mouse_buttons_map_to_win32_xbutton_codes()
    {
        Assert.True(KeyCatalog.Keys.TryGetValue("Mouse Forward (Side)", out var forward));
        Assert.True(KeyCatalog.Keys.TryGetValue("Mouse Back (Side)", out var back));

        Assert.Equal(KeyCatalog.VkXButton2, forward);
        Assert.Equal(KeyCatalog.VkXButton1, back);
    }

    [Theory]
    [InlineData(KeyCatalog.VkXButton2, true)]
    [InlineData(KeyCatalog.VkXButton1, true)]
    [InlineData(0x20u, false)]
    public void IsMouseButton_identifies_side_buttons(uint vk, bool expected) =>
        Assert.Equal(expected, KeyCatalog.IsMouseButton(vk));
}
