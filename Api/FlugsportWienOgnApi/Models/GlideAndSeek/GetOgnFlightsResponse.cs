namespace FlugsportWienOgnApi.Models.GlideAndSeek;

public class GetOgnFlightsResponse
{
    public bool Success { get; set; }
    public IEnumerable<GetOgnFlightsResponseDto> Message { get; set; }
}
