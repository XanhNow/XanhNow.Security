namespace XanhNow.Security.ContractTests.Inventory;

public sealed class ContractInventoryTests
{
    [Fact]
    public void Required_contract_inventory_files_exist()
    {
        var root = FindRepositoryRoot();
        var contractDocs = Path.Combine(root, "docs", "contracts");

        Assert.True(File.Exists(Path.Combine(contractDocs, "rest-v1-contract-inventory.csv")));
        Assert.True(File.Exists(Path.Combine(contractDocs, "rest-v1-route-matrix.csv")));
        Assert.True(File.Exists(Path.Combine(contractDocs, "sensitive-fields.csv")));
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (Directory.Exists(Path.Combine(current, "src")) && Directory.Exists(Path.Combine(current, "docs")))
            {
                return current;
            }

            current = Directory.GetParent(current)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Cannot find repository root.");
    }
}
