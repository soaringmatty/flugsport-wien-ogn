namespace FlugsportWienOgnApi.Models.GlideAndSeek;

public class GetFlightHistoryResponse
{
    public bool Success { get; set; }
    public IEnumerable<List<object>> Message { get; set; }
}
