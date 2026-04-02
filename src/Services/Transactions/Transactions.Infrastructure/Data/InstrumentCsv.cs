namespace Transactions.Infrastructure.Data;
public class InstrumentCsv
{
	public string Ticker { get; set; } = null!;
	public string Name { get; set; } = null!;
	public string Exchange { get; set; } = null!;
	public decimal ConversionRatio { get; set; } = 1;
	public string? Sector { get; set; }
	public string? Region { get; set; }
	public string AssetType { get; set; } = null!;
	public bool IsActive { get; set; } = true;
}