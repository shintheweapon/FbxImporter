using System.Reactive.Linq;
using FbxImporter.ViewModels;
using SoulsAssetPipeline.FLVERImporting;

namespace FbxImporter.Tests;

public class ErTangentsOptionTests
{
    private static FLVER2MaterialInfoBank Bank()
    {
        var bank = new FLVER2MaterialInfoBank();
        bank.MaterialDefs["test.mtd"] = new FLVER2MaterialInfoBank.MaterialDef();
        return bank;
    }

    [Fact]
    public void OptionsDefaultToErTangents()
    {
        Assert.True(new MeshImportOptions().ErTangents);
    }

    [Fact]
    public void DialogStartsTickedWithoutCache()
    {
        var vm = new MeshImportOptionsViewModel("test.mtd", Bank(), null);
        Assert.True(vm.ErTangents);
    }

    [Fact]
    public void DialogKeepsTheCachedChoice()
    {
        var vm = new MeshImportOptionsViewModel("test.mtd", Bank(), new MeshImportOptions { ErTangents = false });
        Assert.False(vm.ErTangents);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ConfirmCarriesTheTickBox(bool ticked)
    {
        var vm = new MeshImportOptionsViewModel("test.mtd", Bank(), null) { ErTangents = ticked };
        MeshImportOptions options = await vm.ConfirmCommand.Execute();
        Assert.Equal(ticked, options.ErTangents);
    }
}
