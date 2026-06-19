using VoiceType.Utils;

namespace VoiceType.Tests;

public class KeyCatalogTests
{
    [Theory]
    [InlineData("Space", 0x20u)]
    [InlineData("A", 0x41u)]
    [InlineData("0", 0x30u)]
    [InlineData("F1", 0x70u)]
    [InlineData("F12", 0x7Bu)]
    public void Keys_MapExpectedVirtualKeyCodes(string name, uint expectedVk)
    {
        Assert.True(KeyCatalog.Keys.TryGetValue(name, out var vk));
        Assert.Equal(expectedVk, vk);
    }

    [Fact]
    public void Keys_AreCaseInsensitive()
    {
        Assert.True(KeyCatalog.Keys.TryGetValue("space", out var lower));
        Assert.True(KeyCatalog.Keys.TryGetValue("SPACE", out var upper));
        Assert.Equal(lower, upper);
    }

    [Theory]
    [InlineData("Mouse 4 (Forward)", KeyCatalog.VkXButton2)]
    [InlineData("Mouse 5 (Back)", KeyCatalog.VkXButton1)]
    [InlineData("Mouse Forward (Side)", KeyCatalog.VkXButton2)]
    [InlineData("Mouse Back (Side)", KeyCatalog.VkXButton1)]
    public void Keys_MapMouseSideButtons(string name, uint expectedVk)
    {
        Assert.True(KeyCatalog.Keys.TryGetValue(name, out var vk));
        Assert.Equal(expectedVk, vk);
    }

    [Theory]
    [InlineData(KeyCatalog.VkXButton2, true)]
    [InlineData(KeyCatalog.VkXButton1, true)]
    [InlineData(0x20u, false)]
    public void IsMouseButton_DetectsSideButtons(uint vk, bool expected)
    {
        Assert.Equal(expected, KeyCatalog.IsMouseButton(vk));
    }

    [Fact]
    public void NameForVk_ReturnsMouseLabel()
    {
        Assert.Equal("Mouse 4 (Forward)", KeyCatalog.NameForVk(KeyCatalog.VkXButton2));
        Assert.Equal("Mouse 5 (Back)", KeyCatalog.NameForVk(KeyCatalog.VkXButton1));
    }

    [Fact]
    public void NameForVk_ReturnsMatchingNameForKeyboardKeys()
    {
        Assert.Equal("Space", KeyCatalog.NameForVk(0x20));
        Assert.Equal("F5", KeyCatalog.NameForVk(0x74));
    }

    [Fact]
    public void NameForVk_UnknownFallsBackToSpace()
    {
        Assert.Equal("Space", KeyCatalog.NameForVk(0xFF));
    }
}
